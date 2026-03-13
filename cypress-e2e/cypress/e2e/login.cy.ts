describe('Login Page', () => {
  beforeEach(() => {
    cy.visit('/sign-in');
  });

  it('displays the login form', () => {
    cy.contains('Welcome').should('be.visible');
    cy.contains('Enter your credentials to sign in').should('be.visible');
    cy.get('input[name="username"]').should('be.visible');
    cy.get('input[name="password"]').should('be.visible');
    cy.get('button[type="submit"]').contains('Sign In').should('be.visible');
  });

  it('shows all test user quick-fill buttons', () => {
    const users = ['backoffice', 'java', 'dotnet', 'multi', 'learner'];
    users.forEach((username) => {
      cy.contains('button', username).should('be.visible');
    });
  });

  it('fills credentials when clicking a test user button', () => {
    cy.contains('button', 'backoffice').click();
    cy.get('input[name="username"]').should('have.value', 'backoffice');
    cy.get('input[name="password"]').should('have.value', 'AdminPassword123!');
  });

  it('fills regular user credentials when clicking test user button', () => {
    cy.contains('button', 'java').click();
    cy.get('input[name="username"]').should('have.value', 'java');
    cy.get('input[name="password"]').should('have.value', 'UserPassword123!');
  });

  it('shows validation errors when submitting empty form', () => {
    cy.get('button[type="submit"]').click();
    cy.contains('Username is required').should('be.visible');
    cy.contains('Password is required').should('be.visible');
  });

  it('shows error for invalid credentials', () => {
    cy.get('input[name="username"]').type('wronguser');
    cy.get('input[name="password"]').type('wrongpassword');
    cy.get('button[type="submit"]').click();
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
