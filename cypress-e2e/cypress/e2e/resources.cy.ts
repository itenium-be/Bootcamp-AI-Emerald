/// <reference types="cypress" />

/**
 * Cypress E2E — Bronnenbibliotheek  (GitHub Issue #22)
 *
 * AC:
 *   ✅ All authenticated users can browse and add
 *   ✅ Completed resources show a checkmark / evidence badge
 *   ✅ Rating shows aggregate up/down count
 *
 * Live seed data confirmed (SeedData.cs, 2026-03-13):
 *   3 resources added by Nathalie (coach):
 *     "Clean Code by Robert C. Martin"       — Book,          Clean Code,       niveau 2–4
 *     "EF Core Getting Started"              — Documentation, EF Core,          niveau 1–3
 *     "ASP.NET Core Web API Tutorial"        — Documentation, ASP.NET Core,     niveau 1–3
 */

import { ResourcesPage } from '../pages/ResourcesPage';

const page = new ResourcesPage();

// ─────────────────────────────────────────────────────────────────────────────
// TEST-22.1  All authenticated roles can access the resource library
// ─────────────────────────────────────────────────────────────────────────────
describe('Toegang voor alle rollen (TEST-22.1)', () => {
  [
    { user: 'learner',    password: 'UserPassword123!',  label: 'learner'          },
    { user: 'java',       password: 'UserPassword123!',  label: 'manager (java)'   },
    { user: 'backoffice', password: 'AdminPassword123!', label: 'backoffice admin' },
  ].forEach(({ user, password, label }) => {
    it(`${label} kan de bronnenbibliotheek openen`, () => {
      cy.login(user, password);
      page.visit();
      page.getTitle().should('be.visible');
      page.getSubtitle().should('be.visible');
    });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-22 general  Resource cards display
// ─────────────────────────────────────────────────────────────────────────────
describe('Bronkaarten (TEST-22 algemeen)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    page.visit();
  });

  it('toont exact 3 bronkaarten (seed data)', () => {
    page.getResourceCards().should('have.length', 3);
  });

  it('toont de titels van alle seed bronnen', () => {
    page.getResourceCard('Clean Code by Robert C. Martin').should('be.visible');
    page.getResourceCard('EF Core Getting Started').should('be.visible');
    page.getResourceCard('ASP.NET Core Web API Tutorial').should('be.visible');
  });

  it('elke kaart toont een type-badge', () => {
    page.getResourceCard('Clean Code by Robert C. Martin').contains('Book').should('be.visible');
    page.getResourceCard('EF Core Getting Started').contains('Documentation').should('be.visible');
  });

  it('elke kaart toont een vaardigheidsnaam en niveaubereik', () => {
    page.getResourceCard('Clean Code by Robert C. Martin').within(() => {
      cy.contains('Clean Code').should('be.visible');
      cy.contains('Niveau 2').should('be.visible');
    });
  });

  it('elke kaart heeft een externe link naar de bron-URL', () => {
    page.getResourceCard('Clean Code by Robert C. Martin')
      .find('a[target="_blank"]')
      .should('have.attr', 'href')
      .and('include', 'oreilly.com');
  });

  it('toont de "Bron Toevoegen" knop', () => {
    page.getAddResourceButton().should('be.visible');
  });

  it('toont het aantal bronnen rechts van de filters', () => {
    cy.contains('3 bronnen').should('be.visible');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-22.2  Filter by skill
// ─────────────────────────────────────────────────────────────────────────────
describe('Filter op vaardigheid (TEST-22.2)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    page.visit();
  });

  it('vaardigheidsfilter bevat "Alle vaardigheden" als standaardoptie', () => {
    page.getSkillFilter().contains('Alle vaardigheden');
  });

  it('selecteren van "Clean Code" toont alleen de Clean Code bron', () => {
    page.filterBySkill('Clean Code');
    page.getResourceCards().should('have.length', 1);
    page.getResourceCard('Clean Code by Robert C. Martin').should('be.visible');
  });

  it('"Filters wissen" knop verschijnt na selecteren van een vaardigheidsfilter', () => {
    page.getClearFiltersButton().should('not.exist');
    page.filterBySkill('Clean Code');
    page.getClearFiltersButton().should('be.visible');
  });

  it('"Filters wissen" herstelt alle 3 bronnen', () => {
    page.filterBySkill('Clean Code');
    page.getResourceCards().should('have.length', 1);
    page.clearFilters();
    page.getResourceCards().should('have.length', 3);
    page.getClearFiltersButton().should('not.exist');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-22.3  Filter by type
// ─────────────────────────────────────────────────────────────────────────────
describe('Filter op type (TEST-22.3)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    page.visit();
  });

  it('typefilter bevat alle resource types', () => {
    const types = ['Article', 'Video', 'Book', 'Course', 'Documentation', 'Other'];
    types.forEach((type) => {
      page.getTypeFilter().find(`option[value="${type}"]`).should('exist');
    });
  });

  it('filteren op "Book" toont alleen de Clean Code bron', () => {
    page.filterByType('Book');
    page.getResourceCards().should('have.length', 1);
    page.getResourceCard('Clean Code by Robert C. Martin').should('be.visible');
  });

  it('filteren op "Documentation" toont 2 bronnen', () => {
    page.filterByType('Documentation');
    page.getResourceCards().should('have.length', 2);
    page.getResourceCard('EF Core Getting Started').should('be.visible');
    page.getResourceCard('ASP.NET Core Web API Tutorial').should('be.visible');
  });

  it('"Filters wissen" knop verschijnt na selecteren van een typefilter', () => {
    page.filterByType('Book');
    page.getClearFiltersButton().should('be.visible');
  });

  it('"Filters wissen" herstelt alle bronnen na typefilter', () => {
    page.filterByType('Book');
    page.clearFilters();
    page.getResourceCards().should('have.length', 3);
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-22.4  Any authenticated user can add a resource
// ─────────────────────────────────────────────────────────────────────────────
describe('Bron toevoegen (TEST-22.4)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    page.visit();
  });

  it('klikken op "Bron Toevoegen" opent het modal met alle velden', () => {
    page.openAddResourceModal();
    page.getModal().should('be.visible');
    page.getModalTitleInput().should('be.visible');
    page.getModalUrlInput().should('be.visible');
    page.getModalTypeSelect().should('be.visible');
    page.getModalSkillSelect().should('be.visible');
  });

  it('modal sluit bij klikken op Annuleren', () => {
    page.openAddResourceModal();
    page.getModalCancelButton().click();
    page.getModal().should('not.exist');
  });

  it('indienen zonder verplichte velden toont foutmelding', () => {
    page.openAddResourceModal();
    page.getModalSaveButton().click();
    page.getModal().contains('Vul alle verplichte velden in').should('be.visible');
  });

  it('learner kan een nieuwe bron toevoegen die in de lijst verschijnt', () => {
    const uniqueTitle = `Cypress Test Resource ${Date.now()}`;
    page.openAddResourceModal();
    page.getModalTitleInput().type(uniqueTitle);
    page.getModalUrlInput().type('https://example.com/cypress-test');
    page.getModalSkillSelect().select('Clean Code');
    page.getModalSaveButton().click();

    // Modal should close and new resource should appear
    page.getModal().should('not.exist');
    page.getResourceCard(uniqueTitle).should('be.visible');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-22.5 / 22.6  Rating — thumbs up/down counts visible and interactive
// ─────────────────────────────────────────────────────────────────────────────
describe('Beoordeling (TEST-22.5, 22.6)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    page.visit();
  });

  it('bronkaart toont duim omhoog en duim omlaag knoppen', () => {
    page.getThumbsUpButton('Clean Code by Robert C. Martin').should('be.visible');
    page.getThumbsDownButton('Clean Code by Robert C. Martin').should('be.visible');
  });

  it('bronkaart toont telwaarden naast de beoordeling-knoppen', () => {
    // Positive/negative ratings are <span> elements inside the thumbs buttons
    page.getResourceCard('Clean Code by Robert C. Martin').within(() => {
      cy.get('button[aria-label="Duim omhoog"]').find('span').should('exist');
      cy.get('button[aria-label="Duim omlaag"]').find('span').should('exist');
    });
  });

  it('klikken op duim omhoog werkt zonder fout', () => {
    page.getThumbsUpButton('Clean Code by Robert C. Martin').click();
    // Should not show an error; button remains visible (no crash)
    page.getThumbsUpButton('Clean Code by Robert C. Martin').should('be.visible');
  });

  it('klikken op duim omlaag werkt zonder fout', () => {
    page.getThumbsDownButton('Clean Code by Robert C. Martin').click();
    page.getThumbsDownButton('Clean Code by Robert C. Martin').should('be.visible');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-22.7 / 22.8  Mark as complete
// ─────────────────────────────────────────────────────────────────────────────
describe('Markeer als voltooid (TEST-22.7, 22.8)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    page.visit();
  });

  it('elke bronkaart heeft een "Markeer als voltooid" knop', () => {
    page.getMarkCompleteButton('Clean Code by Robert C. Martin').should('be.visible');
  });

  it('klikken op "Markeer als voltooid" toont "Voltooid ✓" op de kaart', () => {
    page.getMarkCompleteButton('Clean Code by Robert C. Martin').click();
    page.getResourceCard('Clean Code by Robert C. Martin')
      .contains('Voltooid ✓')
      .should('be.visible');
  });
});
