/// <reference types="cypress" />

/**
 * Page object for /skills/:skillId
 *
 * NOTE: As of 2026-03-13 the detail page does NOT render because skills.tsx
 * is a parent layout route without an <Outlet />.  All methods here are
 * correct for the SkillDetail component — they will work once the bug is fixed.
 */
export class SkillDetailPage {
  getBackLink() {
    return cy.contains('a', 'Terug naar vaardigheden');
  }

  getSkillName() {
    return cy.get('h1');
  }

  getCategoryBadge() {
    return cy.get('main span.rounded-full').first();
  }

  getCheckboxSkillBadge() {
    return cy.contains('Afvinkbare vaardigheid');
  }

  getCheckboxSkillHint() {
    return cy.contains('Markeer als voltooid — geen progressieniveaus');
  }

  /** The "Niveaus" section h2 (only present for multi-level skills) */
  getLevelsSectionHeading() {
    return cy.contains('h2', 'niveaus');
  }

  /** Individual level rows (numbered circles + descriptor text) */
  getLevelRows() {
    return cy.get('main div.space-y-2 > div.rounded-lg');
  }

  /** Label "Niveau N" inside a level row */
  getLevelBadge(niveau: number) {
    return cy.contains('p', `Niveau ${niveau}`);
  }

  getPrerequisitesSectionHeading() {
    return cy.contains('h2', 'Vereiste vaardigheden');
  }

  getPrerequisiteLinks() {
    return cy
      .contains('h2', 'Vereiste vaardigheden')
      .closest('div.space-y-3')
      .find('a[href^="/skills/"]');
  }
}
