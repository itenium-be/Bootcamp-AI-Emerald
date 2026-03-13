/// <reference types="cypress" />

export class SkillCataloguePage {
  visit() {
    cy.visit('/skills');
  }

  getTitle() {
    return cy.contains('h1', 'Vaardighedencatalogus');
  }

  getSubtitle() {
    return cy.contains('Bekijk en verken alle vaardigheden per profiel');
  }

  getSearchInput() {
    return cy.get('input[placeholder="Vaardigheden zoeken..."]');
  }

  getAllCategoriesButton() {
    return cy.contains('button', 'Alles');
  }

  getCategoryButton(category: string) {
    return cy.contains('button', category);
  }

  /** All skill card links (a[href^="/skills/"]) */
  getSkillCards() {
    return cy.get('a[href^="/skills/"]');
  }

  /** Single card by skill name (matches the h2 inside the card) */
  getSkillCard(name: string) {
    return cy.contains('a[href^="/skills/"]', name);
  }

  getNoSkillsMessage() {
    return cy.contains('Geen vaardigheden gevonden');
  }

  getNoSkillsHint() {
    return cy.contains('Pas uw zoekopdracht of categoriefilter aan');
  }

  searchFor(term: string) {
    this.getSearchInput().clear().type(term);
  }

  filterByCategory(category: string) {
    this.getCategoryButton(category).click();
  }

  clearCategoryFilter() {
    this.getAllCategoriesButton().click();
  }
}
