/// <reference types="cypress" />

export class ResourcesPage {
  visit() {
    cy.visit('/resources');
  }

  getTitle() {
    return cy.contains('h1', 'Bronnenbibliotheek');
  }

  getSubtitle() {
    return cy.contains('Blader door leermateriaal bijgedragen door het team');
  }

  getAddResourceButton() {
    return cy.contains('button', 'Bron Toevoegen');
  }

  getResourceCards() {
    return cy.get('.rounded-xl.border.bg-card');
  }

  getResourceCard(title: string) {
    return cy.contains('.rounded-xl.border.bg-card', title);
  }

  getSkillFilter() {
    return cy.get('select').first();
  }

  getTypeFilter() {
    return cy.get('select').eq(1);
  }

  getClearFiltersButton() {
    return cy.contains('button', 'Filters wissen');
  }

  getNoResourcesMessage() {
    return cy.contains('Geen bronnen gevonden');
  }

  getMarkCompleteButton(title: string) {
    return this.getResourceCard(title).contains('button', 'Markeer als voltooid');
  }

  getThumbsUpButton(title: string) {
    return this.getResourceCard(title).find('button[aria-label="Duim omhoog"]');
  }

  getThumbsDownButton(title: string) {
    return this.getResourceCard(title).find('button[aria-label="Duim omlaag"]');
  }

  // Modal helpers
  getModal() {
    return cy.get('.fixed.inset-0');
  }

  getModalTitleInput() {
    return this.getModal().find('input[type="text"]');
  }

  getModalUrlInput() {
    return this.getModal().find('input[type="url"]');
  }

  getModalTypeSelect() {
    return this.getModal().find('select').first();
  }

  getModalSkillSelect() {
    return this.getModal().find('select').last();
  }

  getModalSaveButton() {
    return this.getModal().contains('button', 'Opslaan');
  }

  getModalCancelButton() {
    return this.getModal().contains('button', 'Annuleren');
  }

  openAddResourceModal() {
    this.getAddResourceButton().click();
  }

  filterBySkill(skillName: string) {
    this.getSkillFilter().select(skillName);
  }

  filterByType(type: string) {
    this.getTypeFilter().select(type);
  }

  clearFilters() {
    this.getClearFiltersButton().click();
  }
}
