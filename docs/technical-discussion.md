# Technical Discussion Log

This file is a running, dated log of open-ended technical discussion between
the candidate and the AI assistant — the back-and-forth that's neither a
finalized decision (`decision-log.md`) nor a verbatim prompt (`prompt-log.md`).
Entries are added as the discussion happens, not reconstructed afterward.

---

## 2026-09-18 — Repo setup & verification

Before continuing the build, checked whether the project had already been
pushed to GitHub.

- Fetched `github.com/emilreich?tab=repositories` — confirmed the account
  existed but had 0 public repositories, 0 projects, 0 packages. The
  `alert-hub` repo was still local-only at `C:\dev\alert-hub`.
- Confirmed the working tree was clean (`git status`) and that `client/dist`
  and `client/node_modules` were properly git-ignored (`!!` in
  `git status --ignored`) before pushing anything public.
- The GitHub CLI (`gh`) wasn't installed on this machine. Installed it via
  `winget install --id GitHub.cli`; the first attempt failed against the
  default `msstore` source with a TLS certificate error, retried successfully
  with `--source winget` explicitly.
- Authenticated with `gh auth login --hostname github.com --git-protocol
  https --web` — device code flow, user completed the browser-side
  authorization. Confirmed with `gh auth status`: logged in as `emilreich`,
  `repo` scope granted.
- Created the repo public (`gh repo create alert-hub --public --source=.
  --remote=origin --push`). Repo creation succeeded but the push failed:
  `SSL certificate problem: unable to get local issuer certificate`.
  - Root cause: a system-wide git config
    (`C:\Program Files\Git\etc\gitconfig`) pins
    `http.sslbackend=openssl` with a bundled CA file
    (`mingw64/ssl/certs/ca-bundle.crt`) that doesn't trust whatever's
    intercepting/issuing TLS on this network. Same root-cause class as the
    NuGet feed problem logged in `decision-log.md` D7 — this machine has
    machine-wide config left over from an unrelated environment that
    silently affects tooling unless caught and worked around per-repo.
  - Fix: retried the push with a one-off `-c http.sslbackend=schannel`
    override (`git -c http.sslbackend=schannel push -u origin master`),
    which uses the Windows certificate store instead of the bundled CA file.
    Succeeded. No global or repo config file was modified — the override was
    scoped to that single command.
- Verified the push rather than assuming success from a clean exit code:
  - Diffed local `git ls-files` output against the full GitHub tree API
    (`repos/emilreich/alert-hub/git/trees/master?recursive=true`) —
    byte-for-byte identical file lists.
  - Compared local `git rev-parse HEAD` against the remote
    `refs/heads/master` ref — identical SHA (`2b2ceec5...`).
  - Confirmed commit count on the remote (4) matched local history.
- Result: `https://github.com/emilreich/alert-hub`, public, `master` as
  default branch, all 4 commits present and verified byte-identical to local.

---

## Architecture discussion

### 2026-09-18 — Product requirements, from scratch

The candidate wanted to understand the brief from a product/user-experience
standpoint before touching technical details, and explicitly wanted this
built collaboratively rather than the AI presenting pre-formed conclusions.

**Starting point — separating stated fact from gap.** The brief
(`plan.md`'s opening quote) was re-read line by line and split into what it
actually states versus what it leaves open:

- Explicit, not ambiguous: users self-serve alert setup; the trigger is
  "something important happens in the world"; three example categories are
  given (breaking news, market movements, natural disasters) but flagged as
  illustrative ("that kind of thing"), not exhaustive; email and Slack are
  required at launch; the architecture must make adding channels later easy;
  an admin view is needed.
- Everything else — what "important" means, where events come from, what
  "set up alerts" looks like on screen, user identity, the admin view's
  actual purpose, whether users and admin share one surface or two, per-user
  channel destination info, reliability/volume — is unstated.

**Course-correction.** The first pass at this leaned on the pre-existing
`plan.md`/`architecture.md` decisions (per-user rules, operator-only admin
view, seeded-user dropdown) as the starting framework and presented
options built on top of them. The candidate stopped this and asked to
start from the brief itself and build the product model together, not
inherit earlier assumptions. Recorded here because it's a real
course-correction on process, not just content — subsequent discussion
does not treat the original `plan.md`/`architecture.md` as ground truth.

**Initial product vision, as given by the candidate:**
- Real login for both Users and Admins, with different post-login routing
- Users pick news outlets they trust (multi-select) and, separately,
  categories they're interested in (multi-select) — initially described as
  two independent axes
- Future (explicitly out of scope now): real per-outlet API integration,
  with articles auto-classified into categories as they arrive
- Unsubscribe supported on both axes; user sees a table of current
  subscriptions
- User provides channel config (e.g. "slack email") for where notifications
  should go
- Two user-facing screens: a config screen, and a "my setup + feed" screen
  (current subscriptions plus a readable article feed)
- Admin sees all users and their current setup, can edit on their behalf,
  and can disable users

**Five open questions raised against this vision, and how each resolved:**

1. *How do outlet and category selections combine — AND or OR?* Resolved by
   the candidate reframing the model entirely: subscription is a
   **`(User, Outlet, Category)` pair**, not two independent multi-selects.
   Example given: a user might trust a sports outlet for tennis but not for
   politics. This eliminates the AND/OR question structurally — there is no
   cross-product ambiguity because the pair itself is the unit of
   subscription.
2. *What does "slack email" as channel config actually mean?* Clarified as:
   for the demo, channel destinations are plain free-text config — an email
   address and/or a Slack chat/webhook URL, no verification or real OAuth
   handshake. Future: an OAuth-verified email (Google/Yahoo login) could
   auto-supply the email channel destination instead of manual entry — the
   design shouldn't hardcode manual entry as the only path to a channel
   destination.
3. *Is the feed a new deliverable requiring its own state (read/unread,
   etc.)?* For the demo, a flat list is enough. Read/unread-style per-item
   state is a named future extension — the underlying model should be shaped
   so that's an additive change later, not a rearchitecture.
4. *What's the admin's write scope, and is there an audit trail?* Admin has
   full write access to any user's subscriptions/config. An audit log is a
   named future feature, not built now, but write operations should be
   structured so an audit trail could be attached later without
   restructuring.
5. *How much auth is worth building given the time budget?* Real
   username/password login (real hashing, real sessions) for the demo.
   OAuth (Google/Yahoo/etc.) is an explicit, named future addition — auth
   should treat "how identity was established" as a pluggable concern rather
   than baking password-only assumptions into the core user model.

**Two follow-up clarifications, needed to finalize the subscription/matching
model:**
- *Can one article belong to multiple categories?* Yes. Consequently,
  notification content (email/Slack) should list the article's categories,
  so the user can see why they were notified.
- *What does the outlet/category picker look like on screen?* A drill-down
  flow — pick an outlet, then pick categories for that specific outlet —
  rather than a flat grid/matrix of all outlets × all categories.

**Stub boundary — introduced by the candidate, then pinned down explicitly.**
The candidate raised that article categorization should be designed as a
pluggable seam (interface-shaped, e.g. an `IArticleCategorizer`), explicitly
because it's expected to become a real AI-backed microservice later, and
that "everything in the system should be stubbed" for this demo. This was
clarified into a concrete boundary:
- **Stubbed, behind clean interfaces, swappable later:** article
  categorization, outlet/article ingestion, notification delivery (no real
  SMTP/Slack network calls — simulated/logged only), OAuth login.
- **Fully real, because it's the actual point of the demo:** authentication
  (real hashing, real sessions/roles), the subscription/config CRUD, the
  matching engine, the feed, and the admin CRUD.
- Auth specifically: light hashing effort is fine — the design effort should
  go toward session/role handling and route protection, since the demo's
  stated purpose is to showcase system design, not security hardening.

**New requirements introduced along the way:**
- Admin-driven settings changes must propagate live to the affected user's
  session (not just on next page load) — a genuine real-time/push design
  requirement.
- Basic Docker support, so the system runs with one command.

**Pinning down "immediately reflected" — a real fork in feed design.** Two
readings were identified and put to the candidate directly, since they lead
to materially different designs:
- *(A) Feed as an append-only delivery log* — a subscription change only
  affects future articles; only the live subscriptions/config view updates
  in real time when admin edits a user.
- *(B) Feed as a live, recomputed view over current subscriptions* — a
  subscription change retroactively surfaces previously-published articles
  that now match.

Resolved as **(A), future-only** — no retroactive backfill of the feed on a
subscription change. The real-time requirement covers new matches and live
config visibility, not rewriting feed history. This is a materially simpler
design than (B) while still requiring genuine push infrastructure (not
polling) to satisfy the "immediately reflected" requirement.

**Final two sanity checks:**
- *What does "disable a user" actually do?* Login remains allowed; existing
  feed content stays visible; the feed stops receiving new matches and no
  further notifications are dispatched from the point of disabling onward.
  Framed by the candidate as modeling a future "paused/lapsed paid
  subscription" state — actual billing/tiering is out of scope now, but the
  freeze-not-delete behavior is real and built now.
- *Does admin need to browse the article catalog directly?* No — admin scope
  is strictly user/subscription management (list users, view/edit their
  setup, disable), not content browsing.

This closed out the product-requirements pass. See "Decision Timeline" below
for the terse, ordered list, and the rest of this section for the technical
discussion that follows.

### 2026-09-18 — Technical architecture, topics 1–4

With the product model settled, the discussion moved to the technical side,
topic by topic, at the candidate's request ("I'll let you choose the
topics and we decide together topic by topic"). Proposed order: tech
stack, data model, auth/session, real-time mechanism, stub boundaries,
Docker, API surface, Angular structure.

**Topic 1 — tech stack.** Re-examined rather than silently carried forward
from the pre-conversation `decision-log.md` (D5), given how much had
changed (real auth, live push, Docker, a stub meant to look like a future
microservice). ASP.NET Core + Angular was reconfirmed as still appropriate.
SQLite was challenged — its original appeal ("clone and run, no external
DB") mostly disappears once everything is containerized anyway, and a
client-server DB handles concurrent writes from a background
article-generator better. Proposed switching to Postgres; the candidate
instead chose **MSSQL**, since that's their actual working database — a
direct correction of the specific proposal, not just the general
direction. Also asked whether the categorization seam should be a plain
C# interface or a physically separate process; the candidate wants it
**physically separate — a real microservice**, not just an in-process
abstraction.

**Making the microservice concrete.** Three follow-up questions were
needed: same stack or a different one (Python, given AI/ML tooling
gravity)? The candidate chose to **keep it .NET** (a minimal API project)
for build speed, accepting that physical/network separation alone proves
the architectural point regardless of language. Sync or async call from
the main API? The candidate wants **async** — explicitly so uncategorized
articles are never shown or notified on. Stateless? Not directly asked
again at that point, but implicitly confirmed by the flow that emerged.

**A course-correction on storage, initiated by the candidate.** While
answering the sync/async question, the candidate raised a point not
previously discussed: persisting full article bodies would cause
unbounded data growth. Decision: **store only headline, categories, and
source URL per article — never the body.** Asked to weigh in honestly
(not just reflect the question back), the recommendation given was to
treat this the way real news aggregators do (link out / regenerate on
demand, never rehost), which shaped two further design choices:

- **Async categorization via HTTP request + webhook callback**, not a
  message queue — avoids standing up broker infrastructure to demonstrate
  a pattern being stubbed anyway, while remaining genuinely async and
  decoupled. The candidate agreed but added a condition not originally
  offered as an option: **the dispatch mechanism itself must be a
  configurable/swappable dependency** (`ICategorizationDispatcher`, HTTP
  implementation now, a queue-based implementation a drop-in later via
  config), so the earlier "no broker for now" choice doesn't foreclose one
  later. This mirrors the channel-abstraction pattern from the original
  brief, now applied one layer earlier (to the dispatch mechanism, not
  just the categorizer itself).
- **Full article content served on demand, never stored** — a stub
  `IArticleContentProvider` that deterministically regenerates placeholder
  content from the article's ID, so the same article always shows the same
  fake body without anything being persisted. The candidate confirmed
  wanting **caching** on top of this (a short-lived in-memory cache), so
  repeated views within a window don't redo the (fake) work.

**Topic 2 — data model.** Built out incrementally, entity by entity, with
several rounds of candidate-driven correction:
- `Role` confirmed as a plain enum on `User` (not a table) — no pushback.
- A proposal to skip a `NotificationLog` table (log lines only, since
  delivery is stubbed and admin has no delivery-log screen) was
  **rejected** — the candidate wants a persisted notification log for
  history/activity.
- A proposal for hard-delete unsubscribe was also **rejected** — the
  candidate wants **soft delete** (`IsActive` flag) so subscription history
  is preserved, not erased.
- This led to a scope question: was "notification log" about delivery
  history only, or also an admin-action audit trail? The candidate wants
  **both** — which explicitly reverses an earlier deferral (Turn 19: "we
  will need an audit log but for the demo not yet important"). Flagged
  openly as a scope change, not silently absorbed.
- Design settled on one generic `AuditLog` table (actor vs. target
  columns, an extensible `Action` enum) rather than one table per auditable
  entity type, plus a `NotificationLog` linked to `FeedEntry` (so one
  matched article can fan out to multiple per-channel delivery attempts,
  each with its own status and a destination *snapshot* so later config
  changes don't retroactively alter historical records).
- The candidate then asked for an **admin UI** for the audit trail (not
  just backend data), and specified it should be **embedded in the
  per-user detail view**, not a standalone global activity screen.
- One important connective point surfaced during this topic and confirmed
  correct: `FeedEntry` *must* be a materialized, write-time table rather
  than a live query, because a live join would automatically implement the
  "retroactive" (Option B) feed semantics the candidate had explicitly
  rejected earlier in the product discussion. This is a case of an earlier
  product decision mechanically constraining a later technical one, not
  just informing it.

**Topic 3 — auth & session.** Password hashing: ASP.NET Core's built-in
`PasswordHasher<T>`, not the full Identity framework (avoids
`UserManager`/`SignInManager` ceremony neither needed nor wanted).
Session mechanism: JWT bearer tokens over cookies, reasoned explicitly
from the candidate's stated future intent to add OAuth login later — a
token-based design lets "how identity was established" stay pluggable
without touching authorization code downstream. Two judgment calls were
flagged rather than decided silently — in-memory token storage (not
`localStorage`) despite the refresh-on-page-reload cost, and no
refresh-token flow — and the candidate confirmed both, explicitly framing
the choice around wanting "something deliverable" over
production-completeness.

**Topic 4 — real-time delivery.** SignalR proposed as the natural in-stack
choice, reusing the same JWT for hub authentication. Two push triggers
were designed: a new `FeedEntry` pushes to the matched user
(`Clients.User(userId)`), and an admin-driven config change pushes to the
affected user's own session. The known limitation — `Clients.User(...)`
only reaches connections on the current server process, so multi-instance
scaling would need a Redis backplane — was named explicitly rather than
left implicit. The candidate then added a requirement not previously
scoped: **admin list/detail views should also live-update**, for any
change from any actor (not just changes an admin makes themselves). This
was designed as a SignalR **group** ("Admins," joined based on the JWT
role claim at connection time) that receives a lightweight "this user
changed" broadcast on any subscription/config/disable/audit-log write —
explicitly *not* extended to individual article activity, preserving the
earlier "admin doesn't browse content" boundary.

### 2026-09-18 — Technical architecture, topics 6–8, permissions, and a critical review pass

**Topic 6 — Docker.** Reconsidered mid-discussion by the candidate: Docker
support, originally a named requirement (Turn 25), was downgraded to "the
system should be designed so Docker would fit later, but it isn't a
requirement now." This collapsed the topic from an implementation task
into a design checklist — externalize environment-specific values via
config (connection string, JWT key, the categorization microservice's
base URL, CORS origins), rely on the already-separate-process boundaries
between the three deployables, avoid local-disk assumptions (already true
once MSSQL replaced SQLite), and apply DB schema via migrations at
startup rather than assuming a pre-existing database. None of this
required writing a Dockerfile now — it's a constraint applied to the
other topics rather than a topic of its own.

**Process change requested here:** the candidate asked that
`transcript.md`/`technical-discussion.md` only be updated once the whole
technical discussion finishes, rather than after every topic as had been
happening — a deliberate simplification of the working process, not a
reduction in what gets documented.

**Topic 7 — API surface.** A concrete endpoint list was drawn up across
auth, catalog, self-service ("me"), admin, the service-to-service
categorization callback, a dev-only deterministic ingestion trigger, and
the SignalR hub. Two points were highlighted as intentional design
choices rather than arbitrary REST shape: content is only ever reachable
through a feed entry (`/api/me/feed/{feedEntryId}/content`), not a raw
article ID, which structurally rules out an unintended "browse all
articles" surface; and self-service and admin write endpoints call the
same underlying service methods, with actor/target determined by which
route was hit, so the `AuditLog` design from Topic 2 produces a correct
trail with no duplicated logic between the two code paths. The
categorization callback having no real auth was named again as a known,
accepted simplification.

**A requested deep-dive on the "feed-entry-scoped content" point**
surfaced a genuine implementation-correctness requirement, not just a
design preference: the ownership check must be part of the lookup query
(`WHERE Id = feedEntryId AND UserId = currentUserId`), not a
existence-then-permission-check done separately — otherwise it's an IDOR
(any authenticated user could enumerate feed entry IDs across all users).
A failed lookup must return a uniform `404` regardless of whether the
entry doesn't exist or belongs to someone else, so the two cases aren't
distinguishable from outside (avoids leaking other users' feed activity).
This also connected back cleanly to two earlier decisions: content access
naturally respects "disabled users still see old feed" and "unsubscribed
but historical feed items remain readable," both for free, since
ownership of a `FeedEntry` never expires or depends on current
subscription state. And caching was clarified to stay keyed by
`ArticleId` (not `FeedEntryId`), so two different users matching the same
article share one cached (fake) content generation.

**Topic 8 — Angular structure.** Routing (`AuthGuard`, `RoleGuard`),
screens (`/login`; user's `/config` and tabbed `/home`; admin's user list
and per-user detail with embedded activity), state via plain Angular
signals in a handful of services rather than NgRx (justified by the app's
size and by the real-time push model already being "update a signal,"
which is what a state library would otherwise exist to provide), an HTTP
interceptor for the in-memory JWT, and an explicit note that a `401`
(inevitable with no refresh-token flow) needs a real forced-logout+redirect
handling path, not a silently broken screen. One assumption was flagged
for confirmation rather than assumed silently: that Admin accounts have
no feed/subscriptions of their own.

**A late, substantial course-correction: permissions and roles.** After
all eight topics were nominally done, the candidate introduced a real
authorization redesign — replacing the plain `Role` enum with a proper
`Permission`/`Role`/`RolePermission` model (a user's role grants a set of
named permissions, rather than code branching on a hardcoded role
string), explicitly for maintainability. This was paired with a second,
independent change: admins should be able to navigate to the feed page
and see *all* articles — a direct reversal of the earlier "admin never
browses content" boundary (Turn 29/30), now re-scoped as something a
specific permission (`articles.viewAll`) grants rather than something no
role can ever do. Resolved: one role per user (not multi-role) for
simplicity, permissions resolved once at login and baked into the JWT
(consistent with — and accepting the same staleness tradeoff as — the
rest of the token design), a single `GET /api/feed` endpoint that
branches server-side on the caller's permissions rather than the client
choosing between two endpoints, and two content-access paths (ownership-
scoped and permission-scoped) converging on the same underlying content
provider. The candidate confirmed Admin gets *only* `articles.viewAll` +
`users.manageAny` — no personal subscription/feed layer at all, so
"logged in as Admin" always means view-all mode, not a togglable dual
identity.

**A requested critical-review pass, before moving to plan mode.** Asked
directly to surface real design flaws or bottlenecks rather than declare
the design finished, three findings came out of actually re-tracing the
full request flow end to end:

1. A genuine tension between two independently-made decisions: baking
   permissions into the JWT (accepted staleness tradeoff, fine for rare
   low-stakes changes) had been about to get applied the same way to
   `IsDisabled` — but disabling a user is meant to take effect
   immediately (it's the whole point of the feature), so treating it like
   a cacheable claim would quietly defeat it. Resolved: `IsDisabled`
   checked fresh from the DB on every authenticated request (the
   candidate chose the simpler direct-DB-check over the cache layer
   originally proposed), plus a SignalR forced-logout push to any active
   connection at the moment of disabling (confirmed yes).
2. The newly-added admin "view all articles" mode had no real-time
   trigger — it bypasses `FeedEntry` creation entirely, so the
   push-on-new-article pattern that works for regular users' feeds
   silently didn't extend to it. Resolved: broadcast to a permission-gated
   SignalR group (`articles.viewAll` holders) when an article finishes
   categorization, alongside the existing per-user push.
3. Matching/fan-out running synchronously inside the categorization
   webhook callback was named as a known, deliberately-accepted
   scalability ceiling at this scale (same treatment as the earlier
   SignalR-backplane limitation) — not something to fix now, but not
   something to leave undocumented either.

**Final readiness check.** Asked directly whether the design was ready
for plan mode, the honest answer given was "yes, with a caveat": the
design is internally coherent (re-traced end to end with no
contradictions found) and all ambiguities are resolved, but the overall
scope has grown substantially past the brief's minimal reading (a full
permission system, three distinct SignalR push patterns, a genuine
microservice, full CRUD, an admin UI with embedded audit trail), and
that's a real time-budget risk worth naming rather than assuming away.
Recommended — and the candidate agreed — that the upcoming plan-mode pass
explicitly include a prioritized cut list (what's load-bearing vs. what's
legitimately droppable under time pressure), mirroring the original
`plan.md`'s "what could get cut" section, rather than treating everything
discussed here as equally must-build.

### 2026-09-18 — Moving to implementation, and consolidating documentation

With the technical design settled, the candidate switched into plan mode
to turn it into a concrete build plan. An Explore subagent surveyed the
existing repo first and confirmed there was nothing to preserve — all four
.NET projects were still empty template scaffolding, the Angular client
was the untouched `ng new` output. A Plan subagent then read this file in
full and drafted a concrete plan: an updated 5-project solution layout
(adding a genuinely separate `AlertHub.CategorizationService`), a 12-step
build order (M0–M11) with each step landing as its own commit per the
candidate's explicit requirement, a prioritized load-bearing/droppable/
out-of-scope breakdown addressing the time-budget risk named at the end of
the design discussion, and a testing approach. It also surfaced two
genuine open questions rather than silently resolving them:

1. **Should the categorization microservice share any code with Core?**
   Resolved: fully independent, no project reference, own DTOs — the most
   faithful reading of "physically separate, language-agnostic."
   Consequence, made explicit rather than left implicit: categories cross
   the service boundary as plain strings, kept in sync by hand between the
   microservice's lookup table and the main API's seed data, with no
   compiler check — an accepted tradeoff of true independence, not an
   oversight.
2. **Should SignalR group membership key off the role claim or permission
   claims?** The original design (decision 34) predated the permission
   redesign. Resolved: permission claims, for consistency with "JWT
   permissions are the authorization source of truth" everywhere else.

The plan was approved, and implementation began (M0, then M1 — see
`plan.md`'s milestone table for status and `plan.md`'s "Issues caught
during implementation" section for the concrete bugs found along the way,
including a subagent incidentally deleting `docs/plan.md` during
exploration, caught via routine `git status` review and restored from git
history before anything was committed).

**A significant process decision, separate from the architecture itself:**
partway through implementation, the candidate asked whether
`decision-log.md` and `architecture.md` were still accurate. The honest
answer was mixed — `architecture.md` was almost entirely superseded (it
described the old data model end to end), while `decision-log.md` was a
genuine blend of still-accurate process entries (NuGet feed, EF Core
version pin, template cruft) and architecture-specific entries that were
now stale or flatly contradicted (the old four-project layout, SQLite,
"no end-user portal"). Asked directly "what would you do," the
recommendation was targeted per-entry notes on `decision-log.md` rather
than a blanket banner, since blanket-superseding would incorrectly cast
doubt on the still-valid entries too.

The candidate went further than that recommendation: **consolidate the
repo's documentation down to exactly three living files** —
`plan.md`, `technical-discussion.md` (this file), and `transcript.md` —
deleting `architecture.md`, `decision-log.md`, and `prompt-log.md`
entirely rather than archiving or merging them. Before executing this,
two concerns were raised explicitly rather than silently complying: that
`decision-log.md`'s still-valid process entries would be lost with no
successor location, and — more materially — that `prompt-log.md` was the
**only** record of the earlier (pre-this-conversation) session's actual
prompts, which the exercise's own instructions require submitting. Given
that context, the candidate confirmed deleting all three anyway,
including `prompt-log.md`, as a deliberate choice. `plan.md` was rewritten
from scratch as the living implementation plan (context, resolved
ambiguities, solution layout, milestone status table, priority/cut list,
testing approach, verification checkpoints) rather than left with its
pre-redesign content.

This resurfaced practically almost immediately: fixing a stale reference
in `README.md` (it still called the Angular app "admin view" only, among
other staleness) led to writing a cross-reference to `technical-discussion.md`
for "the full story of issues caught during the build" — which turned out
to be wrong, since that story only ever lived in the now-deleted
`decision-log.md`. Caught and corrected before it shipped, and it raised
a real question: with `decision-log.md` gone, where do future build-time
catches go, if not scattered only through git log? The candidate wanted
them scannable outside of git log specifically, so `plan.md` (already the
living status/what's-being-built document) gained an "Issues caught during
implementation" section, backfilled with everything that would otherwise
have been lost (the NuGet feed issue, the EF Core version pin, template
cruft, the subagent plan.md deletion, and the `InvariantGlobalization`/
`SqlClient` bug found during M1) plus a place for new entries going
forward.

---

## Decision Timeline

Terse, chronological, decision-point-only view of the discussion above (and
of the technical discussion to follow) — for quickly scanning what was
decided and in what order, without re-reading the full narrative. Each item
corresponds to a point in the "Architecture discussion" section above.

1. Two distinct personas hide behind the brief's word "users" — alert
   recipients (end users) and operators (admin) — and the brief conflates
   them; they need separate treatment.
2. Course-correction: build the product model from the brief itself,
   collaboratively, rather than from the pre-existing `plan.md`/
   `architecture.md` decisions.
3. Real login + role-based post-login routing for User vs. Admin —
   supersedes the earlier seeded-user-dropdown approach.
4. Subscription unit is `(User, Outlet, Category)` pairs, not independent
   outlet/category multi-selects — resolves the AND/OR combination question
   structurally.
5. An article can belong to multiple categories; a user matches an article
   if any of the article's categories overlaps with a pair they hold for
   that article's outlet.
6. Notification content lists the article's categories.
7. Subscription picker UI is a drill-down (pick outlet, then categories for
   that outlet) — not a flat grid.
8. Channel config is simple free-text for the demo (email address, Slack
   chat/webhook URL); OAuth-verified email as an alternate future source is
   a named extension, not built now.
9. Feed is a flat list for now; per-item read/unread state is a named
   future extension.
10. Admin has full write access to user configs; audit logging is deferred
    but writes should be structured to allow it later.
11. Auth is real username/password with light hashing; OAuth is deferred;
    design effort is focused on sessions/roles rather than hashing
    strength.
12. Article categorization is a stubbed, pluggable seam, explicitly
    anticipating a future AI-backed microservice.
13. Full stub boundary agreed: outlet ingestion, categorization,
    notification delivery, and OAuth are stubbed; auth, subscriptions/
    matching/feed/admin CRUD are fully real.
14. Notification delivery is fully stubbed — simulated/logged only, no real
    SMTP/Slack network calls.
15. New requirement: admin-driven settings changes must propagate live to
    the affected user's session, not just on refresh.
16. Feed real-time semantics resolved as future-only (append-log style) —
    no retroactive backfill when subscriptions change.
17. Disable-user behavior: login stays allowed, existing feed stays
    visible, but the feed stops updating and notifications stop dispatching
    from the point of disabling — models a "paused" state.
18. Admin scope confirmed as user/subscription management only, no article
    browsing.
19. New infra requirement: basic Docker support for a one-command run.
20. Tech stack (ASP.NET Core + Angular) reconfirmed as still appropriate
    given the expanded scope.
21. Database switched from SQLite to MSSQL — Postgres was proposed, but
    MSSQL chosen since it's the candidate's actual working database.
22. Article categorization is built as a genuinely separate, physically
    isolated microservice (not just an in-process interface) — kept as a
    minimal ASP.NET Core API rather than a different-language stub, for
    build speed.
23. Categorization is asynchronous: an article is never shown, matched, or
    notified on until categorization completes.
24. Async categorization uses an HTTP request + webhook callback pattern
    rather than a message broker, to avoid infra overhead for a stubbed
    workload — but the dispatch mechanism is abstracted behind
    `ICategorizationDispatcher` so a queue-based implementation is a
    swappable addition later, not a rewrite.
25. Articles are stored lightly — headline, outlet, categories, source
    URL, no body — to keep storage flat regardless of catalog size.
26. Full article content is served on demand via a stub
    `IArticleContentProvider` that deterministically regenerates
    placeholder content (never persisted), backed by a short-lived
    in-memory cache.
27. Full data model settled: `User`, `UserChannelConfig`, `Outlet`,
    `Category`, `Subscription`, `Article`, `ArticleCategory`, `FeedEntry`,
    `NotificationLog`, `AuditLog`.
28. `FeedEntry` must be a materialized, write-time table rather than a
    live query — a direct technical consequence of the earlier
    "future-only" feed decision, not just a performance choice.
29. `Role` stays a plain enum on `User`, not a separate roles table.
30. `Subscription` unsubscribe is a soft toggle (`IsActive` + timestamps)
    rather than row deletion, to preserve history.
31. A `NotificationLog` (delivery history) and a general-purpose
    `AuditLog` (actor vs. target distinguishes self-service edits from
    admin-initiated ones) are both built now — reversing the earlier
    deferral of an audit log.
32. The audit trail gets a real UI, embedded in the per-user admin detail
    view rather than a standalone global activity screen.
32a. Docker downgraded from a requirement to a design constraint: the
    system should be config-driven and process-separated enough to
    containerize later, but no Dockerfile/compose is built now.
32b. Process change: `transcript.md`/`technical-discussion.md` updated in
    one batch at the end of the technical discussion, not after every
    topic.
32c. API surface settled; content is reachable only via
    `/api/me/feed/{feedEntryId}/content`, never a raw article ID —
    structurally rules out an unscoped "browse all articles" endpoint.
32d. Feed-entry content-ownership check must be part of the lookup query
    itself (not a separate existence-then-permission check), and a
    failed lookup returns a uniform 404 regardless of cause — closing a
    real IDOR-shaped risk identified during review, not just a style
    preference.
32e. Content-provider caching confirmed keyed by `ArticleId`, not
    `FeedEntryId`, so multiple users matching the same article share one
    cached (fake) content generation.
32f. Angular: routing/guards, screens, and state via plain signals (not
    NgRx) settled; a `401` (inevitable with no refresh flow) gets a real
    forced-logout+redirect path, not a silent failure.
33. Auth: ASP.NET Core's built-in `PasswordHasher<T>` (not the full
    Identity framework); JWT bearer tokens (not cookies), chosen
    specifically because a token-based session generalizes cleanly to
    future OAuth logins; held in memory on the Angular client (not
    `localStorage`); no refresh-token flow.
34. Real-time delivery uses SignalR: a single hub, per-user targeted
    pushes for new feed items and a user's own config changes, plus a
    broadcast to an "Admins" SignalR group (any user/subscription/
    config/audit change, from any actor) so admin list/detail views
    live-update too — explicitly not extended to individual article
    activity, keeping admin's "no content browsing" boundary intact.
35. Acknowledged, not solved: SignalR's per-server-instance user targeting
    means a future multi-instance deployment would need a backplane (e.g.
    Redis) — a named limitation, not a gap papered over.
36. The always-on periodic `BackgroundService` for article ingestion was
    dropped — challenged directly by the candidate ("do we really need
    this?"), and on reconsideration it added real cost (Docker lifecycle
    complexity, non-deterministic timing) for no architectural benefit
    over an on-demand trigger.
37. Article generation, seed data, and categorization all became fully
    deterministic — fixed seed templates (outlet, headline, source URL,
    expected categories) instead of random generation; the categorizer
    stub does deterministic keyword/lookup matching instead of random
    category assignment. Driven by the candidate naming testing as a
    first-class concern, not an afterthought.
38. Course-correction on evaluation criteria: the candidate clarified the
    actual evaluation is the code and the discussion/decision trail, not a
    live demo walkthrough. This retroactively invalidated the "artificial
    delay for demo visibility" idea entirely (no delay, in any mode) —
    flagged explicitly as a case of optimizing for the wrong thing
    (legibility-when-run instead of legibility-when-read), rather than
    silently patched over. Other decisions justified on similar
    demo-optics grounds were re-checked; the on-demand-trigger-over-timer
    choice held up for independent reasons (determinism, testability) and
    didn't need to change.
39. `Role` enum replaced with a `Permission`/`Role`/`RolePermission`
    model — a role grants a named set of permissions, rather than code
    branching on a hardcoded role string. One role per user (not
    multi-role) to start.
40. Reversal of Turn 18: admins can now browse *all* articles via the
    feed page, gated by a new `articles.viewAll` permission rather than
    being permanently out of scope — `GET /api/feed` branches server-side
    on the caller's permissions instead of the client picking an
    endpoint.
41. Permissions are resolved once at login and baked into the JWT,
    accepting the same staleness-until-expiry tradeoff already accepted
    for the rest of the token design.
42. Admin gets exactly `articles.viewAll` + `users.manageAny` — no
    personal subscription/feed permissions at all; "logged in as Admin"
    always means view-all mode, never a togglable dual identity.
43. Critical-review finding, resolved: `IsDisabled` must NOT be baked
    into the JWT like permissions are — disabling needs to take effect
    immediately, so it's checked fresh from the DB on every authenticated
    request (a direct per-request check, not a cache layer), plus a
    SignalR forced-logout push to any active connection at the moment of
    disabling.
44. Critical-review finding, resolved: admin's "view all articles" mode
    gets its own real-time trigger — a broadcast to an
    `articles.viewAll`-permission SignalR group when an article finishes
    categorization, alongside the existing per-user push (the earlier
    real-time design predated this permission and hadn't been revisited
    for it).
45. Critical-review finding, accepted as a named/documented limitation
    rather than fixed: matching/fan-out runs synchronously inside the
    categorization webhook callback — fine at this scale, would need to
    move to a queued/background step at real scale.
46. Before moving to plan mode, agreed the plan should include an
    explicit, ordered priority list (load-bearing vs. legitimately
    cuttable under time pressure) — the technical design is internally
    coherent, but the total scope has grown past the brief's minimal
    reading and that risk should be named, not assumed away.
47. Categorization microservice confirmed fully independent — no shared
    code/project reference with `Core`; categories cross the service
    boundary as plain strings, manually kept in sync, as an accepted
    tradeoff of true independence.
48. SignalR group membership resolved to key off permission claims, not
    the role claim, for consistency with the post-permission-redesign
    authorization model.
49. Implementation plan approved: 12 milestones (M0–M11), each its own
    commit, with an explicit load-bearing/droppable/out-of-scope priority
    list addressing the time-budget risk.
50. Documentation consolidated to exactly three living docs — `plan.md`,
    `technical-discussion.md`, `transcript.md`. `architecture.md`,
    `decision-log.md`, and `prompt-log.md` deleted entirely (not
    archived), including `prompt-log.md` despite it being the only record
    of the earlier session's prompts — a deliberate choice, made after
    that tradeoff was explicitly named rather than silently absorbed.
51. `plan.md` gained a scannable "Issues caught during implementation"
    section (backfilling what `decision-log.md` used to hold), after a
    stale `README.md` reference revealed that build-time catches had no
    home once `decision-log.md` was deleted — git log alone wasn't
    considered scannable enough.

*(Further entries appended here, matching new headings/entries added above,
as the technical-side discussion proceeds.)*
