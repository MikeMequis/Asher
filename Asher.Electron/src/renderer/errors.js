/**
 * @param {unknown} err
 * @returns {{ kind: 'host' | 'application' | 'communication' | 'validation', message: string }}
 */
export function classifyError(err) {
  const message = err instanceof Error ? err.message : String(err);
  const code = err instanceof Error ? err.code : undefined;

  if (message.includes('Host is not available')) {
    return {
      kind: 'host',
      message: 'Cannot connect to the Asher application. Check the connection and try again.'
    };
  }

  if (code) {
    return {
      kind: 'application',
      message: message || 'The application could not complete this operation.'
    };
  }

  return {
    kind: 'communication',
    message: 'Communication with the application failed. Please try again.'
  };
}
