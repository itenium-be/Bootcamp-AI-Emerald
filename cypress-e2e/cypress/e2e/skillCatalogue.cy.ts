/// <reference types="cypress" />

/**
 * Cypress E2E — Vaardighedencatalogus  (GitHub Issue #12)
 *
 * AC:
 *   ✅ All authenticated roles can access
 *   ✅ Skills are shown grouped by category with filters
 *   ⚠️  Skill detail shows all level descriptors clearly       (@bug — see below)
 *   ⚠️  Prerequisite skills are clickable                      (@bug — see below)
 *
 * Live data confirmed via Playwright MCP (http://localhost:5173, 2026-03-13):
 *   Total skills : 21
 *   Architecture & Design  : Clean Code, Dependency Injection                (2)
 *   Data & Persistence     : Entity Framework Core, Hibernate / JPA          (2)
 *   Language & Runtime     : C#, Java, Java Streams, LINQ, .NET              (5)
 *   Testing                : JUnit 5, Testing Fundamentals, xUnit            (3)
 *   Tooling & DevOps       : AWS, Azure, CI/CD, Docker, Git, Maven           (6)
 *   Web & API              : ASP.NET Core, Spring Boot, Spring Security      (3)
 *   Checkbox skill (levelCount=1): Git — /skills/2
 *
 * ⚠️  ROUTING BUG discovered via Playwright MCP:
 *   Navigating to /skills/:id shows the catalogue instead of the skill detail.
 *   Root cause: skills.tsx acts as a parent layout route, but SkillCatalogue
 *   has no <Outlet />, so SkillDetail never mounts.
 *   Fix: add <Outlet /> to SkillCatalogue, or move the catalogue to
 *        routes/_authenticated/skills.index.tsx.
 *   Tests that depend on the detail page are marked "@bug" — they will be RED
 *   until the fix lands (TDD: red → green).
 */

import { SkillCataloguePage } from '../pages/SkillCataloguePage';
import { SkillDetailPage } from '../pages/SkillDetailPage';

const catalogue = new SkillCataloguePage();
const detail = new SkillDetailPage();

// ─────────────────────────────────────────────────────────────────────────────
// TEST-12.1 / 12.2 / 12.3  All authenticated roles can access the catalogue
// ─────────────────────────────────────────────────────────────────────────────
describe('Toegang voor alle rollen (TEST-12.1, 12.2, 12.3)', () => {
  [
    { user: 'learner',    password: 'UserPassword123!',  label: 'learner'          },
    { user: 'java',       password: 'UserPassword123!',  label: 'manager (java)'   },
    { user: 'backoffice', password: 'AdminPassword123!', label: 'backoffice admin' },
  ].forEach(({ user, password, label }) => {
    it(`${label} kan de vaardighedencatalogus openen`, () => {
      cy.login(user, password);
      catalogue.visit();
      catalogue.getTitle().should('be.visible');
      catalogue.getSubtitle().should('be.visible');
    });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-12.4  Skills shown with category badges
// ─────────────────────────────────────────────────────────────────────────────
describe('Vaardigheidkaarten (TEST-12.4)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    catalogue.visit();
  });

  it('toont exact 21 vaardigheidkaarten', () => {
    catalogue.getSkillCards().should('have.length', 21);
  });

  it('elke kaart toont een categorie-badge en vaardigheidsnaam', () => {
    catalogue.getSkillCards().each(($card) => {
      cy.wrap($card).within(() => {
        cy.get('span').first().invoke('text').should('have.length.greaterThan', 0);
        cy.get('h2').should('be.visible');
      });
    });
  });

  it('alle 6 categorie-filterchips zijn zichtbaar naast "Alles"', () => {
    ['Architecture & Design', 'Data & Persistence', 'Language & Runtime',
     'Testing', 'Tooling & DevOps', 'Web & API'].forEach((cat) => {
      catalogue.getCategoryButton(cat).should('be.visible');
    });
    catalogue.getAllCategoriesButton().should('be.visible');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-12.5  Category filter
// ─────────────────────────────────────────────────────────────────────────────
describe('Categorie-filter (TEST-12.5)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    catalogue.visit();
  });

  it('"Architecture & Design" → 2 vaardigheden, allemaal met die badge', () => {
    catalogue.filterByCategory('Architecture & Design');
    catalogue.getSkillCards().should('have.length', 2);
    catalogue.getSkillCards().each(($card) => {
      cy.wrap($card).contains('Architecture & Design');
    });
  });

  it('"Testing" → 3 vaardigheden', () => {
    catalogue.filterByCategory('Testing');
    catalogue.getSkillCards().should('have.length', 3);
  });

  it('"Language & Runtime" → 5 vaardigheden', () => {
    catalogue.filterByCategory('Language & Runtime');
    catalogue.getSkillCards().should('have.length', 5);
  });

  it('"Tooling & DevOps" → 6 vaardigheden', () => {
    catalogue.filterByCategory('Tooling & DevOps');
    catalogue.getSkillCards().should('have.length', 6);
  });

  it('"Alles" herstelt alle 21 vaardigheden', () => {
    catalogue.filterByCategory('Testing');
    catalogue.getSkillCards().its('length').should('be.lessThan', 21);
    catalogue.clearCategoryFilter();
    catalogue.getSkillCards().should('have.length', 21);
  });

  it('actieve categorieknop krijgt primaire achtergrondkleur', () => {
    catalogue.filterByCategory('Testing');
    catalogue.getCategoryButton('Testing')
      .should('have.class', 'bg-primary')
      .and('have.class', 'text-primary-foreground');
    catalogue.getCategoryButton('Architecture & Design')
      .should('not.have.class', 'bg-primary');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-12.5b / 12.5c  Search
// ─────────────────────────────────────────────────────────────────────────────
describe('Zoekfunctie (TEST-12.5b, 12.5c)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    catalogue.visit();
  });

  it('zoeken op "Clean" toont alleen Clean Code', () => {
    catalogue.searchFor('Clean');
    catalogue.getSkillCards().should('have.length', 1);
    catalogue.getSkillCard('Clean Code').should('be.visible');
  });

  it('zoeken op "java" toont vaardigheden die "java" bevatten', () => {
    catalogue.searchFor('java');
    catalogue.getSkillCards().its('length').should('be.greaterThan', 0);
    catalogue.getSkillCards().each(($card) => {
      cy.wrap($card).invoke('text').invoke('toLowerCase').should('include', 'java');
    });
  });

  it('geen resultaten → lege staat met hulptekst (TEST-12.5c)', () => {
    catalogue.searchFor('xqznowaythisexists99887766');
    catalogue.getNoSkillsMessage().should('be.visible');
    catalogue.getNoSkillsHint().should('be.visible');
    catalogue.getSkillCards().should('not.exist');
  });

  it('zoek + categoriefilter combineren correct (xUnit binnen Testing)', () => {
    catalogue.filterByCategory('Testing');
    catalogue.searchFor('xUnit');
    catalogue.getSkillCards().should('have.length', 1);
    catalogue.getSkillCard('xUnit').should('be.visible');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-12.9  Checkbox skill on the catalogue card
// ─────────────────────────────────────────────────────────────────────────────
describe('Afvinkbare vaardigheid op cataloguskaart (TEST-12.9)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    catalogue.visit();
  });

  it('Git-kaart toont "Afvinkbare vaardigheid" badge', () => {
    catalogue.getSkillCard('Git').within(() => {
      cy.contains('Afvinkbare vaardigheid').should('be.visible');
    });
  });

  it('Git-kaart toont geen "niveaus" tekst', () => {
    catalogue.getSkillCard('Git').within(() => {
      cy.contains('niveaus').should('not.exist');
    });
  });

  it('multi-niveau kaarten tonen het juiste aantal niveaus', () => {
    catalogue.getSkillCard('Clean Code').within(() => { cy.contains('5 niveaus').should('be.visible'); });
    catalogue.getSkillCard('C#').within(() => { cy.contains('7 niveaus').should('be.visible'); });
    catalogue.getSkillCard('Docker').within(() => { cy.contains('3 niveaus').should('be.visible'); });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-12.6  Clicking a skill navigates to /skills/:id
// URL changes correctly; detail content is blocked by the routing bug below.
// ─────────────────────────────────────────────────────────────────────────────
describe('Navigatie naar detailpagina — URL (TEST-12.6)', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
    catalogue.visit();
  });

  it('klikken op Clean Code wijzigt URL naar /skills/1', () => {
    catalogue.getSkillCard('Clean Code').click();
    cy.url().should('include', '/skills/1');
  });

  it('klikken op Git wijzigt URL naar /skills/2', () => {
    catalogue.getSkillCard('Git').click();
    cy.url().should('include', '/skills/2');
  });

  // @bug — SkillCatalogue has no <Outlet />, SkillDetail never mounts
  it('@bug — detailpagina toont "Terug"-link en vaardigheidsnaam als h1', () => {
    catalogue.getSkillCard('Clean Code').click();
    cy.url().should('include', '/skills/1');
    detail.getBackLink().should('be.visible');
    detail.getSkillName().should('contain.text', 'Clean Code');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-12.7  Skill detail shows all level descriptors  (@bug)
// ─────────────────────────────────────────────────────────────────────────────
describe('Niveaubeschrijvingen op detailpagina (TEST-12.7) — @bug', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
  });

  it('@bug — Clean Code detail: back-link, h1, 5 genummerde niveauregels', () => {
    cy.visit('/skills/1');
    detail.getBackLink().should('be.visible');
    detail.getSkillName().should('contain.text', 'Clean Code');
    detail.getLevelsSectionHeading().should('be.visible');
    detail.getLevelRows().should('have.length', 5);
    for (let i = 1; i <= 5; i++) {
      detail.getLevelBadge(i).should('be.visible');
    }
  });

  it('@bug — C# detail: 7 niveauregels', () => {
    cy.visit('/skills/6');
    detail.getSkillName().should('contain.text', 'C#');
    detail.getLevelRows().should('have.length', 7);
  });

  it('@bug — "Terug naar vaardigheden" navigeert terug naar /skills', () => {
    cy.visit('/skills/1');
    detail.getBackLink().click();
    cy.url().should('match', /\/skills$/);
    catalogue.getTitle().should('be.visible');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-12.9 (detail)  Checkbox skill detail page  (@bug)
// ─────────────────────────────────────────────────────────────────────────────
describe('Afvinkbare vaardigheid — detailpagina (TEST-12.9 detail) — @bug', () => {
  it('@bug — Git detail: checkbox-badge zichtbaar, geen niveaus-sectie', () => {
    cy.login('learner', 'UserPassword123!');
    cy.visit('/skills/2');
    detail.getCheckboxSkillBadge().should('be.visible');
    detail.getCheckboxSkillHint().should('be.visible');
    detail.getLevelsSectionHeading().should('not.exist');
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// TEST-12.8  Prerequisite skills are clickable links  (@bug)
// ─────────────────────────────────────────────────────────────────────────────
describe('Vereiste vaardigheden zijn klikbaar (TEST-12.8) — @bug', () => {
  beforeEach(() => {
    cy.login('learner', 'UserPassword123!');
  });

  it('@bug — Entity Framework Core detail: vereiste vaardigheden als links', () => {
    cy.visit('/skills/9');
    detail.getPrerequisitesSectionHeading().should('be.visible');
    detail.getPrerequisiteLinks().should('have.length.greaterThan', 0);
    detail.getPrerequisiteLinks().each(($link) => {
      cy.wrap($link).should('have.attr', 'href').and('match', /\/skills\/\d+/);
    });
  });

  it('@bug — klikken op vereiste vaardigheid opent diens detailpagina', () => {
    cy.visit('/skills/9');
    detail.getPrerequisiteLinks().first().then(($link) => {
      const expectedHref = $link.attr('href') ?? '';
      cy.wrap($link).click();
      cy.url().should('include', expectedHref);
      detail.getSkillName().should('be.visible');
    });
  });
});
