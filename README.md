Bootcamp AI
===========

Creating the **SkillForge**

For the powerpoint, existing docs/skillmatrices and Midjourney pictures,
see the [Bootcamp-AI-Resources Repository](https://github.com/itenium-be/Bootcamp-AI-Resources)

Missing Prep
------------

Create a `%USERPROFILE%\.npmrc` file with content using your Github token (Personal access tokens (classic)).

```ini
//npm.pkg.github.com/:_authToken=ghp_xxx
@itenium-forge:registry=https://npm.pkg.github.com
```

Non Windows may need an additional: `sudo apt-get install libatomic1`


Start Locally
-------------

```sh
cp .env.example .env
# Add GITHUB_USER and GITHUB_TOKEN
docker compose up -d --build
```



Development Kickoff
-------------------

### Before everyone can start — the baseline

Three issues must land on `master` before any other work branches off.
All 5 devs should review #1 before it merges — it is the only PR where everyone must have input,
because changing the entity model later is painful across 4 parallel branches.

| Order | Issue | Why it blocks everyone |
|-------|-------|------------------------|
| 1st | [#1 DB schema & EF Core migrations](https://github.com/itenium-be/Bootcamp-AI-Emerald/issues/1) | Defines the shared entity model — all epics code against it |
| 2nd | [#2 Auth role mapping](https://github.com/itenium-be/Bootcamp-AI-Emerald/issues/2) + [#32 Team-scoped repo enforcement](https://github.com/itenium-be/Bootcamp-AI-Emerald/issues/32) | Every controller and endpoint needs role + team scoping in place |
| 3rd | [#6 Seed data](https://github.com/itenium-be/Bootcamp-AI-Emerald/issues/6) | Without seed data, every dev gets a blank screen on first run |

**Suggested Day 1 sequence:**

```
Morning:   all 5 devs review and agree on schema (#1) → merge
Afternoon: #2 + #32 implemented and merged (one dev, ~half day each)
Evening:   #6 seed data merged

Day 2:     everyone branches off their own epic
```

Before Day 2, agree as a team on:
- **API conventions** — response envelope shape, error format, pagination style
- **Branch strategy** — one branch per issue (`feature/issue-42-...`), PR back to `master`, no long-lived epic branches

### Epics & issue overview

| Epic | Owner | Issues | Total SP |
|------|-------|--------|----------|
| [E1 Foundation & Infrastructure](https://github.com/itenium-be/Bootcamp-AI-Emerald/milestone/1) | Dev 1 | #1 #2 #3 #4 #5 #6 #32 | 39 |
| [E2 Skill Catalogue](https://github.com/itenium-be/Bootcamp-AI-Emerald/milestone/2) | Dev 2 | #7 #8 #9 #10 #11 #12 | 27 |
| [E3 Roadmap & Seniority](https://github.com/itenium-be/Bootcamp-AI-Emerald/milestone/3) | Dev 3 | #13 #14 #15 #16 #17 | 24 |
| [E4 Goals, Resources & Readiness](https://github.com/itenium-be/Bootcamp-AI-Emerald/milestone/4) | Dev 4 | #18 #19 #20 #21 #22 #23 #30 | 28 |
| [E5 Coach Dashboard & Live Session](https://github.com/itenium-be/Bootcamp-AI-Emerald/milestone/5) | Dev 5 | #24 #25 #26 #27 #28 #29 #31 | 33 |

**Total: 32 issues — 151 story points**

Epics 2–5 can run in parallel from Day 2. E3/E4 frontend work can start immediately using stub/mock data while waiting for backend APIs.


Backlog
-------

### User & Access Management

Focus: Backoffice administration

- As a user, I want to sign in with SSO or login/pwd.
- As backoffice, I want to manage user roles (learner, team manager, backoffice) so access is controlled.
- As backoffice, I want to manage the different teams.
- As backoffice, I want to assign learners to teams.
- As team manager, I want to see my team members (learners) so I can track their learning.
- As backoffice, I want to see user activity (login history, last active).
- As backoffice, I want to deactivate users (soft delete) without losing their history.
- As a user, I want to reset my password via email.


### Course Catalog & Content

Focus: Course creation and browsing

- Course Catalog
    - As a learner, I want to browse published courses so I can choose what to learn.
    - As a learner, I want to search and filter courses by topic, level, and status (mandatory/optional).
    - As team manager, I want to create, edit, publish and archive courses.
    - As team manager, I want to assign mandatory/optional courses to my team and to individual members.
- Content Management
    - As team manager, I want to add learning content (text, images, video, PDF, links, embedded youtube).
    - As team manager, I want to structure courses into modules (a "path" to a goal, multiple courses are one module).
    - As team manager, I want to structure courses into lessons (multiple lessons are one course).
    - As team manager, I want to update content without affecting completed learners.
- Course Visualization
    - As a learner I want to see the modules and my completion rate of the courses therein.
    - As a learner I want to see the lessons inside a course and my completion rate therein.


### Enrollment & Learning Experience

Focus: Learner journey

- Learning Experience
    - As a learner, I want to enroll in a course so I can start learning.
    - As a learner, I want to resume where I left off so I don’t lose progress.
    - As a learner, I want to mark lessons as new / done / later.
- Progress & Tracking
    - As a learner, I want to see completed courses so I know what I’ve finished.
    - As team manager, I want to see my team’s learning progress.
    - As backoffice, I want reporting on course usage and completion rates.
- Feedback
    - As a learner, I want to provide lesson and course feedback, so content can improve.
    - As backoffice, I want to review learner feedback per course.
    - A a learner, I want to suggest additional learning content.
    - As team manager, I want to approve suggested learning content.
    - As a learner, I want to add context (personal experience, content rating, ...) to a lesson for other learners.


### Assessments & Quizzes

Focus: Knowledge validation

- As a learner, I want to complete quizzes so I can validate my knowledge.
- As team manager, I want to create quizzes with multiple question types.
- As team manager, I want to configure pass/fail criteria (score, attempts, time limit).
- As a learner, I want to receive feedback after completing an assessment.
- As team manager, I want to see quiz analytics (most missed questions, average scores).
