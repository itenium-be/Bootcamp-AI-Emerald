export class LoginPage {
  visit() {
    cy.visit('/sign-in');
  }

  getUsernameInput() {
    return cy.get('input[name="username"]');
  }

  getPasswordInput() {
    return cy.get('input[name="password"]');
  }

  getSubmitButton() {
    return cy.get('button[type="submit"]');
  }

  getQuickFillButton(username: string) {
    return cy.contains('button', username);
  }

  typeUsername(username: string) {
    this.getUsernameInput().type(username);
  }

  typePassword(password: string) {
    this.getPasswordInput().type(password);
  }

  submit() {
    this.getSubmitButton().click();
  }

  clickQuickFill(username: string) {
    this.getQuickFillButton(username).click();
  }

  login(username: string, password: string) {
    this.typeUsername(username);
    this.typePassword(password);
    this.submit();
  }
}
