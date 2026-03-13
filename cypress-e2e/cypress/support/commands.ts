/// <reference types="cypress" />

import { LoginPage } from '../pages/LoginPage';

const API_BASE_URL = 'http://localhost:5000';

declare global {
  namespace Cypress {
    interface Chainable {
      login(username: string, password: string): Chainable<void>;
    }
  }
}

/**
 * Login command — bypasses the UI for speed and reliability.
 *
 * Posts directly to the OpenIddict token endpoint, then writes the JWT into
 * localStorage in the Zustand persist format the app expects.
 * Wrapped in cy.session() so the token request only runs once per
 * username/password pair per test run; subsequent tests restore the cached
 * localStorage state instantly.
 *
 * The UI-based login (LoginPage) is intentionally kept below for reference
 * and is still used by login.cy.ts which tests the sign-in page itself.
 */
Cypress.Commands.add('login', (username: string, password: string) => {
  cy.session(
    [username, password],
    () => {
      cy.request({
        method: 'POST',
        url: `${API_BASE_URL}/connect/token`,
        form: true,
        body: {
          grant_type: 'password',
          username,
          password,
          client_id: 'skillforge-spa',
          scope: 'openid profile email',
        },
      }).then(({ body }) => {
        const token: string = body.access_token;

        // Mirror the JWT parsing logic from authStore.ts
        const payload = JSON.parse(atob(token.split('.')[1]));
        const roles: string[] = Array.isArray(payload.role)
          ? payload.role
          : payload.role
            ? [payload.role]
            : [];

        const user = {
          id: payload.sub,
          email: payload.email || payload.preferred_username || '',
          name: payload.name || payload.preferred_username || 'User',
          isBackOffice: roles.includes('backoffice'),
        };

        // Write Zustand persist state directly — same key and shape as authStore
        window.localStorage.setItem(
          'auth-storage',
          JSON.stringify({
            state: { accessToken: token, user, isAuthenticated: true },
            version: 0,
          }),
        );
      });
    },
    {
      validate() {
        // Re-run setup if the auth key is gone (e.g. cleared between tests)
        cy.wrap(window.localStorage.getItem('auth-storage')).should('not.be.null');
      },
    },
  );
});

export { LoginPage };
export {};
