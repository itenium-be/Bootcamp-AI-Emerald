import { LoginPage } from '../pages/LoginPage';

declare global {
  namespace Cypress {
    interface Chainable {
      login(username: string, password: string): Chainable<void>;
    }
  }
}

Cypress.Commands.add('login', (username: string, password: string) => {
  const loginPage = new LoginPage();
  loginPage.visit();
  loginPage.login(username, password);
});

export {};
