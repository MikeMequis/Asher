/*
 * Asher PoC - minimal native bootstrap for the Mono runtime embedded in DustAET (Linux).
 *
 * Goal: prove that native code loaded into the running Dust process can reach the
 * already-initialized Mono runtime and execute an external managed assembly.
 *
 * This file does NOT create a Mono runtime, does NOT patch the game and does NOT
 * reverse engineer anything. It only resolves the public Mono embedding API that the
 * Dust executable already exposes and drives it.
 *
 * Build: see ../build.sh
 * Load : LD_PRELOAD=/abs/path/libasher_bootstrap.so ./DustAET
 *
 * Optional environment variables:
 *   ASHER_BOOTSTRAP_ASSEMBLY   absolute/relative path to the managed DLL
 *                              (default: BootstrapTest.dll next to this .so, then cwd)
 *   ASHER_BOOTSTRAP_NAMESPACE  default "AsherBootstrapTest"
 *   ASHER_BOOTSTRAP_CLASS      default "Bootstrap"
 *   ASHER_BOOTSTRAP_METHOD     default "Initialize"
 *   ASHER_BOOTSTRAP_AUTORUN    "0" disables the automatic background bootstrap
 *   ASHER_BOOTSTRAP_TIMEOUT_MS max wait for the runtime (0 = wait forever), default 60000
 */

#define _GNU_SOURCE

#include <dlfcn.h>
#include <pthread.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

/* ------------------------------------------------------------------ */
/* Opaque Mono handles and function signatures (public embedding API)  */
/* ------------------------------------------------------------------ */

typedef void MonoDomain;
typedef void MonoThread;
typedef void MonoAssembly;
typedef void MonoImage;
typedef void MonoClass;
typedef void MonoMethod;
typedef void MonoObject;
typedef int MonoImageOpenStatus;

typedef MonoDomain *(*fn_get_root_domain)(void);
typedef MonoThread *(*fn_thread_attach)(MonoDomain *domain);
typedef void (*fn_thread_detach)(MonoThread *thread);
typedef MonoAssembly *(*fn_assembly_open)(const char *name, MonoImageOpenStatus *status);
typedef MonoImage *(*fn_assembly_get_image)(MonoAssembly *assembly);
typedef MonoClass *(*fn_class_from_name)(MonoImage *image, const char *name_space, const char *name);
typedef MonoMethod *(*fn_class_get_method_from_name)(MonoClass *klass, const char *name, int param_count);
typedef MonoObject *(*fn_runtime_invoke)(MonoMethod *method, void *obj, void **params, MonoObject **exc);

typedef struct {
    fn_get_root_domain get_root_domain;
    fn_thread_attach thread_attach;
    fn_thread_detach thread_detach;
    fn_assembly_open assembly_open;
    fn_assembly_get_image assembly_get_image;
    fn_class_from_name class_from_name;
    fn_class_get_method_from_name class_get_method_from_name;
    fn_runtime_invoke runtime_invoke;
} MonoApi;

/* ------------------------------------------------------------------ */
/* Diagnostics helpers                                                 */
/* ------------------------------------------------------------------ */

static void log_stage(const char *fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    fputs("[AsherPoC] ", stdout);
    vprintf(fmt, args);
    fputc('\n', stdout);
    fflush(stdout);
    va_end(args);
}

static void log_fail(const char *fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    fputs("[AsherPoC] FAILED: ", stderr);
    vfprintf(stderr, fmt, args);
    fputc('\n', stderr);
    fflush(stderr);
    va_end(args);
}

static void log_dlerror(const char *what)
{
    const char *err = dlerror();
    log_fail("%s (%s)", what, err ? err : "no dlerror detail");
}

/* ------------------------------------------------------------------ */
/* Symbol resolution                                                   */
/* ------------------------------------------------------------------ */

/*
 * Resolve all Mono entry points from the global symbol scope of the current
 * process. The Dust executable already exports the Mono embedding API, so no
 * hardcoded addresses, dlopen of libmono, or /proc tricks are used.
 *
 * Returns 0 while any symbol is still missing (the caller retries), 1 once all
 * symbols resolved.
 */
static int resolve_mono_api(MonoApi *api)
{
    memset(api, 0, sizeof(*api));

    api->get_root_domain = (fn_get_root_domain)dlsym(RTLD_DEFAULT, "mono_get_root_domain");
    api->thread_attach = (fn_thread_attach)dlsym(RTLD_DEFAULT, "mono_thread_attach");
    api->thread_detach = (fn_thread_detach)dlsym(RTLD_DEFAULT, "mono_thread_detach");
    api->assembly_open = (fn_assembly_open)dlsym(RTLD_DEFAULT, "mono_assembly_open");
    api->assembly_get_image = (fn_assembly_get_image)dlsym(RTLD_DEFAULT, "mono_assembly_get_image");
    api->class_from_name = (fn_class_from_name)dlsym(RTLD_DEFAULT, "mono_class_from_name");
    api->class_get_method_from_name =
        (fn_class_get_method_from_name)dlsym(RTLD_DEFAULT, "mono_class_get_method_from_name");
    api->runtime_invoke = (fn_runtime_invoke)dlsym(RTLD_DEFAULT, "mono_runtime_invoke");

    if (!api->get_root_domain || !api->thread_attach || !api->thread_detach ||
        !api->assembly_open || !api->assembly_get_image || !api->class_from_name ||
        !api->class_get_method_from_name || !api->runtime_invoke) {
        return 0;
    }
    return 1;
}

/* ------------------------------------------------------------------ */
/* Default assembly location                                           */
/* ------------------------------------------------------------------ */

/*
 * When ASHER_BOOTSTRAP_ASSEMBLY is not set, look for BootstrapTest.dll next to
 * this shared object. This only uses dladdr (no /proc, no memory scanning).
 */
static void default_assembly_path(char *buffer, size_t size)
{
    Dl_info info;
    if (dladdr((void *)&default_assembly_path, &info) && info.dli_fname) {
        const char *slash = strrchr(info.dli_fname, '/');
        if (slash) {
            size_t dir_len = (size_t)(slash - info.dli_fname);
            if (dir_len + 1 + strlen("BootstrapTest.dll") + 1 <= size) {
                memcpy(buffer, info.dli_fname, dir_len);
                buffer[dir_len] = '/';
                strcpy(buffer + dir_len + 1, "BootstrapTest.dll");
                return;
            }
        }
    }
    snprintf(buffer, size, "%s", "BootstrapTest.dll");
}

static const char *env_or(const char *name, const char *fallback)
{
    const char *value = getenv(name);
    return (value && value[0]) ? value : fallback;
}

/* ------------------------------------------------------------------ */
/* Bootstrap                                                           */
/* ------------------------------------------------------------------ */

static void run_bootstrap(void)
{
    MonoApi api;
    MonoDomain *domain;
    MonoThread *thread;
    MonoAssembly *assembly;
    MonoImage *image;
    MonoClass *klass;
    MonoMethod *method;
    MonoObject *exception = NULL;
    MonoImageOpenStatus status = 0;

    const char *assembly_path;
    const char *the_namespace = env_or("ASHER_BOOTSTRAP_NAMESPACE", "AsherBootstrapTest");
    const char *class_name = env_or("ASHER_BOOTSTRAP_CLASS", "Bootstrap");
    const char *method_name = env_or("ASHER_BOOTSTRAP_METHOD", "Initialize");
    char default_path[4096];
    long timeout_ms = atol(env_or("ASHER_BOOTSTRAP_TIMEOUT_MS", "60000"));
    long waited_ms = 0;

    log_stage("Native bootstrap loaded");

    /* Stage 1: resolve Mono symbols. May take a moment if libmono is loaded late. */
    while (!resolve_mono_api(&api)) {
        if (timeout_ms > 0 && waited_ms >= timeout_ms) {
            log_dlerror("Mono symbol resolution timed out waiting for mono_get_root_domain and friends");
            return;
        }
        usleep(50 * 1000);
        waited_ms += 50;
    }
    log_stage("Mono symbols resolved");

    /* Stage 2: obtain the root domain, which only exists once Dust has initialized Mono. */
    domain = NULL;
    while ((domain = api.get_root_domain()) == NULL) {
        if (timeout_ms > 0 && waited_ms >= timeout_ms) {
            log_fail("Root domain acquisition timed out (mono_get_root_domain returned NULL)");
            return;
        }
        usleep(50 * 1000);
        waited_ms += 50;
    }
    log_stage("Root domain acquired");

    /* Stage 3: attach this native thread to the existing runtime. */
    thread = api.thread_attach(domain);
    if (!thread) {
        log_fail("Thread attachment failed (mono_thread_attach returned NULL)");
        return;
    }
    log_stage("Thread attached");

    /* Stage 4: load the external managed assembly. */
    assembly_path = getenv("ASHER_BOOTSTRAP_ASSEMBLY");
    if (!assembly_path || !assembly_path[0]) {
        default_assembly_path(default_path, sizeof(default_path));
        assembly_path = default_path;
    }

    assembly = api.assembly_open(assembly_path, &status);
    if (!assembly) {
        log_fail("Assembly loading failed (mono_assembly_open '%s', status=%d)", assembly_path, status);
        api.thread_detach(thread);
        return;
    }
    log_stage("Assembly loaded");

    image = api.assembly_get_image(assembly);
    if (!image) {
        log_fail("Assembly image resolution failed (mono_assembly_get_image returned NULL)");
        api.thread_detach(thread);
        return;
    }

    /* Stage 5: find the test class. */
    klass = api.class_from_name(image, the_namespace, class_name);
    if (!klass) {
        log_fail("Class resolution failed (%s.%s)", the_namespace, class_name);
        api.thread_detach(thread);
        return;
    }

    /* Stage 6: find the test method (static, zero parameters). */
    method = api.class_get_method_from_name(klass, method_name, 0);
    if (!method) {
        log_fail("Method resolution failed (%s.%s.%s)", the_namespace, class_name, method_name);
        api.thread_detach(thread);
        return;
    }
    log_stage("Method resolved");

    /* Stage 7: invoke. obj=NULL selects the static method. */
    api.runtime_invoke(method, NULL, NULL, &exception);
    if (exception) {
        log_fail("Method invocation threw a managed exception");
        api.thread_detach(thread);
        return;
    }

    api.thread_detach(thread);
    log_stage("Bootstrap completed successfully");
}

static pthread_once_t g_run_once = PTHREAD_ONCE_INIT;

/* Public entry point, also callable by a future host instead of autorun. */
void asher_bootstrap_run(void)
{
    pthread_once(&g_run_once, run_bootstrap);
}

/* ------------------------------------------------------------------ */

static void *autorun_thread(void *unused)
{
    (void)unused;
    asher_bootstrap_run();
    return NULL;
}

__attribute__((constructor)) static void asher_bootstrap_ctor(void)
{
    pthread_t thread;
    const char *autorun;

    /* Keep our diagnostics visible even when stdout is a pipe. */
    setvbuf(stdout, NULL, _IOLBF, 0);

    autorun = getenv("ASHER_BOOTSTRAP_AUTORUN");
    if (autorun && strcmp(autorun, "0") == 0) {
        return;
    }

    if (pthread_create(&thread, NULL, autorun_thread, NULL) != 0) {
        log_fail("Could not start the bootstrap thread");
        return;
    }
    pthread_detach(thread);
}
