import createClient from 'openapi-fetch';
import type { paths } from './schema';

export const api = createClient<paths>({ baseUrl: window.location.origin });

/**
 * Account HTTP endpoints are delivered with P3-1. Keeping the deferred calls
 * here gives the session store one networking boundary without inventing DTOs
 * before the generated OpenAPI schema contains those operations.
 */
export const accountApi = {
  me: async (): Promise<null> => null,
  logout: async (): Promise<void> => undefined,
};
