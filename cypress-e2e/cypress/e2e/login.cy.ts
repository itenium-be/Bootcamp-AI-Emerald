import { LoginPage } from '../pages/LoginPage';

describe('Login Page', () => {
  const loginPage = new LoginPage();

  beforeEach(() => {
    loginPage.visit();
  });

  it('displays the login form', () => {
    cy.contains('Welkom').should('be.visible');
    cy.contains('Voer uw gegevens in om in te loggen').should('be.visible');
    loginPage.getUsernameInput().should('be.visible');
    loginPage.getPasswordInput().should('be.visible');
    loginPage.getSubmitButton().should('be.visible');
  });

  it('shows all test user quick-fill buttons', () => {
    const users = ['backoffice', 'java', 'dotnet', 'multi', 'learner'];
    users.forEach((username) => {
      loginPage.getQuickFillButton(username).should('be.visible');
    });
  });

  it('fills credentials when clicking a test user button', () => {
    loginPage.clickQuickFill('backoffice');
    loginPage.getUsernameInput().should('have.value', 'backoffice');
    loginPage.getPasswordInput().should('have.value', 'AdminPassword123!');
  });

  it('fills regular user credentials when clicking test user button', () => {
    loginPage.clickQuickFill('java');
    loginPage.getUsernameInput().should('have.value', 'java');
    loginPage.getPasswordInput().should('have.value', 'UserPassword123!');
  });

  it('shows validation errors when submitting empty form', () => {
    loginPage.submit();
    cy.contains('Gebruikersnaam is verplicht').should('be.visible');
    cy.contains('Wachtwoord is verplicht').should('be.visible');
  });

  it('shows error for invalid credentials', () => {
    loginPage.login('wronguser', 'wrongpassword');
    cy.contains('Invalid username or password').should('be.visible');
  });

  it('logs in successfully as backoffice admin', () => {
    cy.login('backoffice', 'AdminPassword123!');
    cy.url().should('not.include', '/sign-in');
  });

  it('logs in successfully as a regular user', () => {
    cy.login('java', 'UserPassword123!');
    cy.url().should('not.include', '/sign-in');
  });

  it('redirects unauthenticated user to sign-in', () => {
    cy.visit('/');
    cy.url().should('include', '/sign-in');
  });
});
