(() => {
  "use strict";

  const app = document.getElementById("app");
  const currentPage = document.body.dataset.page || "landing";
  const charts = [];

  const courseData = [
    { code: "CSE 3201", title: "Software Develoment Project", credits: 3, faculty: "Md. Rashed Miah", initials: "FR", progress: 68, color: "", status: "On track" },
    { code: "CSE 3203", title: "Project Management Enterprice", credits: 3, faculty: "Tasnia Harun", initials: "AH", progress: 54, color: "teal", status: "Needs focus" },
    { code: "CSE 3205", title: "Computer Networks", credits: 3, faculty: "Nazma Akter", initials: "NJ", progress: 76, color: "purple", status: "On track" },
    { code: "CSE 3207", title: "Software Engineering", credits: 3, faculty: "Sazid Ahmed", initials: "TA", progress: 82, color: "orange", status: "Strong" },
    { code: "CSE 3209", title: "Computer Cyber Security", credits: 3, faculty: "Ayesha Banu", initials: "IK", progress: 47, color: "red", status: "At risk" },
    { code: "CSE 3210", title: "Data Communication", credits: 1.5, faculty: "Prof. Fazlul Kader", initials: "FR", progress: 72, color: "teal", status: "On track" }
  ];

  const assessmentData = [
    { type: "Quiz", title: "Relational Algebra Quiz", course: "Software Develoment Project", marks: "8 / 10", deadline: "Completed · Aug 11", status: "Graded", badge: "success", icon: "bi-patch-question" },
    { type: "CT", title: "Process Scheduling CT", course: "Project Management Enterprice", marks: "— / 20", deadline: "Aug 18 · 10:30 AM", status: "Due soon", badge: "danger", icon: "bi-pencil-square" },
    { type: "Assignment", title: "Campus Network Design", course: "Computer Networks", marks: "— / 15", deadline: "Aug 21 · 11:59 PM", status: "In progress", badge: "warning", icon: "bi-file-earmark-text" },
    { type: "Midterm", title: "Mid-Semester Examination", course: "Software Engineering", marks: "31 / 40", deadline: "Completed · Aug 04", status: "Graded", badge: "success", icon: "bi-journal-check" },
    { type: "Final", title: "Final Examination", course: "Computer Cyber Security", marks: "— / 60", deadline: "Sep 28 · 09:00 AM", status: "Upcoming", badge: "primary", icon: "bi-mortarboard" },
    { type: "Assignment", title: "SQL Query Optimization", course: "Software Engineering Lab", marks: "13 / 15", deadline: "Completed · Aug 09", status: "Graded", badge: "success", icon: "bi-file-earmark-code" },
    { type: "Quiz", title: "TCP/IP Fundamentals", course: "Computer Networks Lab", marks: "— / 10", deadline: "Aug 25 · 02:00 PM", status: "Upcoming", badge: "primary", icon: "bi-patch-question" }
  ];

  const resourcesData = [
    { type: "pdf", icon: "bi-file-earmark-pdf", label: "PDF", title: "PESTLE — Complete Guide", course: "Project Management Enterprice", author: "Dr. Farhana Rahman", meta: "2.8 MB · 24 pages", saved: true },
    { type: "video", icon: "bi-play-circle", label: "Video", title: " Agile Explained", course: "Software Engineering", author: "Academic Media Lab", meta: "28 min · 1.2k views", saved: false },
    { type: "note", icon: "bi-journal-richtext", label: "Notes", title: "OSI Model — Exam Revision Notes", course: "Computer Networks", author: "Nafisa Chowdhury", meta: "Updated 2 days ago", saved: true },
    { type: "pdf", icon: "bi-file-earmark-pdf", label: "PDF", title: "Software Testing & Quality Assurance", course: "Software Engineering", author: "Tasnim Ahmed", meta: "3.4 MB · 31 pages", saved: false },
    { type: "video", icon: "bi-play-circle", label: "Video", title: "Types of Attack", course: "Computer Cyber Security", author: "Dr. Imran Kabir", meta: "41 min · 860 views", saved: false },
    { type: "note", icon: "bi-journal-richtext", label: "Notes", title: "SQL Joins — Visual Cheat Sheet", course: "Software Develoment Project", author: "StudyPilot Community", meta: "Saved by 347 students", saved: true }
  ];

  const pageMeta = {
    dashboard: ["Student Dashboard", "Good afternoon, Najmul Alam Efti. Here is your academic pulse for today."],
    profile: ["Student Profile", "Review your academic identity, goals, and study preferences."],
    courses: ["My Courses", "Track every course, faculty member, credit, and completion milestone."],
    assessments: ["Assessments", "Stay ahead of quizzes, CTs, assignments, midterms, and finals."],
    planner: ["GPA & Smart Planner", "Calculate outcomes and turn your targets into an achievable weekly plan."],
    priorities: ["Course Priorities", "Focus your study time where it can make the greatest academic impact."],
    progress: ["Progress & Weak Topics", "See your momentum, mastery, productivity, and areas that need attention."],
    resources: ["Resource Center", "Find trusted PDFs, videos, and notes mapped to your current courses."],
    community: ["Academic Community", "Ask questions, share knowledge, and learn with focused study groups."],
    faculty: ["Faculty Dashboard", "Monitor course performance and support students before they fall behind."],
    events: ["University Events", "Discover events, manage registrations, and collect participation certificates."]
  };

  const navigation = [
    { label: "Overview", items: [
      ["dashboard.html", "dashboard", "bi-grid-1x2", "Student Dashboard"],
      ["profile.html", "profile", "bi-person", "My Profile"]
    ]},
    { label: "Academics", items: [
      ["courses.html", "courses", "bi-journals", "Courses"],
      ["assessments.html", "assessments", "bi-clipboard2-check", "Assessments", "3"],
      ["planner.html", "planner", "bi-calculator", "GPA & Planner"],
      ["priorities.html", "priorities", "bi-lightning-charge", "Priorities"]
    ]},
    { label: "Growth", items: [
      ["progress.html", "progress", "bi-graph-up-arrow", "Progress & Insights"],
      ["resources.html", "resources", "bi-collection-play", "Resources"],
      ["community.html", "community", "bi-people", "Community"]
    ]},
    { label: "Campus", items: [
      ["events.html", "events", "bi-calendar-event", "Events", "New"],
      ["faculty.html", "faculty", "bi-easel2", "Faculty View"]
    ]}
  ];

  const brand = (compact = false) => `
    <span class="brand">
      <span class="brand-mark"><i class="bi bi-mortarboard-fill"></i></span>
      <span class="brand-copy">StudyPilot${compact ? "" : "<small>Academic success</small>"}</span>
    </span>`;

  const loader = () => `
    <div class="page-loader" id="pageLoader" aria-live="polite" aria-label="Loading page">
      <div class="loader-mark"><div class="loader-ring"></div><i class="bi bi-mortarboard-fill loader-cap"></i></div>
    </div>`;

  const landingPage = () => `
    ${loader()}
    <div class="landing-page">
      <nav class="landing-nav">
        <div class="container py-3 d-flex align-items-center gap-3">
          <a href="index.html" aria-label="Academic StudyPilot home">${brand()}</a>
          <div class="nav-menu d-flex align-items-center gap-1 mx-auto">
            <a class="nav-link" href="#features">Features</a>
            <a class="nav-link" href="#benefits">Why StudyPilot</a>
            <a class="nav-link" href="#stories">Student stories</a>
          </div>
          <button class="btn btn-icon btn-ghost theme-toggle" type="button" aria-label="Switch color theme"><i class="bi bi-moon-stars"></i></button>
          <a class="btn btn-ghost" href="faculty.html">Faculty view</a>
          <a class="btn btn-primary" href="dashboard.html">Student View <i class="bi bi-arrow-up-right ms-1"></i></a>
        </div>
      </nav>

      <main>
        <section class="hero text-center">
          <div class="container">
            <span class="eyebrow"><i class="bi bi-stars"></i> Built for ambitious university students</span>
            <h1 class="hero-title">Turn academic pressure into <span class="gradient-text">clear progress.</span></h1>
            <p class="hero-copy">StudyPilot brings performance, GPA goals, smart planning, weak-topic insights, trusted resources, and campus life into one calm academic workspace.</p>
            <div class="hero-actions">
              <a class="btn btn-primary btn-lg" href="dashboard.html">Explore student dashboard <i class="bi bi-arrow-right ms-2"></i></a>
              <a class="btn btn-ghost btn-lg" href="#features"><i class="bi bi-play-circle me-2"></i> See what it can do</a>
            </div>
            <div class="trust-row">
              <span><i class="bi bi-check-circle-fill"></i> No setup required</span>
              <span><i class="bi bi-check-circle-fill"></i> Realistic demo data</span>
              <span><i class="bi bi-check-circle-fill"></i> Student-first design</span>
            </div>

            <div class="hero-preview" aria-label="Student dashboard preview">
              <div class="preview-toolbar"><span></span><span></span><span></span></div>
              <div class="preview-shell">
                <div class="preview-side">
                  <div class="preview-side-logo"></div>
                  <div class="preview-side-line active"></div>
                  <div class="preview-side-line"></div><div class="preview-side-line"></div><div class="preview-side-line"></div><div class="preview-side-line"></div>
                </div>
                <div class="preview-main">
                  <div class="preview-heading"></div>
                  <div class="preview-stats"><div class="preview-stat"></div><div class="preview-stat"></div><div class="preview-stat"></div><div class="preview-stat"></div></div>
                  <div class="preview-grid"><div class="preview-chart"></div><div class="preview-list"><span></span><span></span><span></span><span></span></div></div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section class="landing-section" id="features">
          <div class="container">
            <div class="text-center mb-5">
              <div class="section-kicker">Everything in one academic cockpit</div>
              <h2 class="section-title">Know what matters. Study with purpose.</h2>
              <p class="section-copy">A focused set of tools designed around the decisions students make every week—not another generic task manager.</p>
            </div>
            <div class="row g-4">
              ${[
                ["bi-speedometer2", "Academic command center", "See CGPA, assessments, priorities, streaks, and weekly momentum at a glance."],
                ["bi-bullseye", "Target CGPA planner", "Turn a long-term CGPA target into the semester results and credits you need."],
                ["bi-calendar2-week", "Smart study planning", "Balance study blocks, revision, and restorative breaks in a realistic weekly plan."],
                ["bi-search-heart", "Weak-topic detection", "Spot concepts that repeatedly reduce marks and get a focused recovery path."],
                ["bi-collection-play", "Curated resources", "Find course-specific PDFs, videos, and notes without hunting through scattered groups."],
                ["bi-people", "Academic community", "Learn with course communities, questions, and small accountability study groups."]
              ].map(item => `<div class="col-md-6 col-xl-4"><article class="feature-card"><div class="feature-icon"><i class="bi ${item[0]}"></i></div><h3>${item[1]}</h3><p>${item[2]}</p></article></div>`).join("")}
            </div>
          </div>
        </section>

        <section class="landing-section soft" id="benefits">
          <div class="container">
            <div class="row align-items-center g-5">
              <div class="col-lg-6">
                <div class="benefit-panel">
                  <span class="eyebrow text-white border-white border-opacity-10" style="background:rgba(255,255,255,.08)"><i class="bi bi-graph-up-arrow"></i> Your momentum, visible</span>
                  <h3 class="mt-4 fw-bold" style="max-width:330px;font-size:2rem;letter-spacing:-.04em">Small study decisions become measurable progress.</h3>
                  <div class="benefit-metric one"><small>Focus score</small><strong>86%</strong><small><i class="bi bi-arrow-up"></i> 12% this week</small></div>
                  <div class="benefit-metric two"><small>Study consistency</small><strong>11-day streak</strong><small>Personal best this semester</small></div>
                </div>
              </div>
              <div class="col-lg-6">
                <div class="section-kicker">Designed for better decisions</div>
                <h2 class="section-title text-start mx-0">A calmer way to stay academically in control.</h2>
                <p class="text-muted mb-4">StudyPilot connects results, time, topics, and deadlines so students can respond early—before a difficult week becomes a difficult semester.</p>
                <div class="benefit-list">
                  ${[
                    ["See risk early", "Priority scoring combines deadlines, progress, marks, and topic confidence."],
                    ["Plan around real capacity", "Study blocks include revision and break reminders instead of unrealistic all-day schedules."],
                    ["Keep goals grounded", "GPA and CGPA calculations show the exact assumptions behind every target."],
                    ["Bring support closer", "Faculty insights, resources, groups, and events are connected to the academic journey."]
                  ].map(item => `<div class="benefit-item"><span class="benefit-check"><i class="bi bi-check2"></i></span><div><strong>${item[0]}</strong><div class="text-muted small mt-1">${item[1]}</div></div></div>`).join("")}
                </div>
              </div>
            </div>
          </div>
        </section>

        <section class="landing-section">
          <div class="container"><div class="stats-strip"><div class="row g-4">
            <div class="col-6 col-lg-3"><div class="landing-stat"><strong>12.8k</strong><span>study plans completed</span></div></div>
            <div class="col-6 col-lg-3"><div class="landing-stat"><strong>84%</strong><span>weekly goal completion</span></div></div>
            <div class="col-6 col-lg-3"><div class="landing-stat"><strong>3.2×</strong><span>faster weak-topic response</span></div></div>
            <div class="col-6 col-lg-3"><div class="landing-stat"><strong>4.8/5</strong><span>student demo rating</span></div></div>
          </div></div></div>
        </section>

        <section class="landing-section soft" id="stories">
          <div class="container">
            <div class="text-center mb-5"><div class="section-kicker">Student stories</div><h2 class="section-title">Built around real academic pressure.</h2></div>
            <div class="row g-4">
              ${[
                ["NR", "Nafisa Rahman", "CSE · 3rd year", "The priority dashboard showed me that Operating Systems—not the course I was spending most time on—was the real risk. My weekly plan finally feels honest.", ""],
                ["SZ", "Samiul Zaman", "EEE · 2nd year", "I like that the CGPA planner explains the numbers. It makes the target feel practical instead of just showing a motivational message.", "teal"],
                ["TM", "Tahmina Morshed", "BBA · 4th year", "The event tickets, resources, and study groups make it feel like a complete university platform, not only a marks calculator.", "orange"]
              ].map(item => `<div class="col-md-4"><article class="testimonial-card"><div class="quote-mark">“</div><p>${item[3]}</p><div class="person"><span class="avatar ${item[4]}">${item[0]}</span><div><strong class="d-block small">${item[1]}</strong><span class="text-muted small">${item[2]}</span></div></div></article></div>`).join("")}
            </div>
          </div>
        </section>

        <section class="landing-section">
          <div class="container"><div class="cta-panel"><div class="section-kicker text-info">Prototype experience</div><h2 class="section-title text-white">See the full academic journey in action.</h2><p class="mx-auto mb-4" style="max-width:580px;color:#94a3b8">Explore a realistic student workspace with live calculators, filters, charts, events, and theme controls.</p><a class="btn btn-primary btn-lg" href="dashboard.html">Launch interactive demo <i class="bi bi-arrow-right ms-2"></i></a></div></div>
        </section>
      </main>

      <footer class="landing-footer">
        <div class="container">
          <div class="row g-4 pb-4">
            <div class="col-lg-5"><a href="index.html">${brand()}</a><p class="mt-3" style="max-width:390px;color:#64748b">A university academic success platform that connects performance, planning, progress, people, and opportunity.</p></div>
            <div class="col-6 col-lg-2 footer-links"><strong class="d-block text-white mb-3">Platform</strong><a href="dashboard.html">Dashboard</a><a href="planner.html">GPA planner</a><a href="resources.html">Resources</a></div>
            <div class="col-6 col-lg-2 footer-links"><strong class="d-block text-white mb-3">Campus</strong><a href="community.html">Community</a><a href="events.html">Events</a><a href="faculty.html">Faculty view</a></div>
            <div class="col-lg-3 footer-links"><strong class="d-block text-white mb-3">Prototype note</strong><p style="color:#64748b;font-size:.82rem">This Phase 1 demonstration uses fictional students and realistic mock academic data.</p></div>
          </div>
          <div class="d-flex flex-wrap justify-content-between gap-2 pt-4 border-top" style="border-color:#1e293b!important;color:#64748b;font-size:.75rem"><span>© 2026 Academic StudyPilot</span><span>Designed for university academic success</span></div>
        </div>
      </footer>
    </div>
    <div class="toast-stack" id="toastStack" aria-live="polite"></div>`;

  const sidebar = () => navigation.map(group => `
    <div class="sidebar-label">${group.label}</div>
    ${group.items.map(item => `<a class="sidebar-link ${currentPage === item[1] ? "active" : ""}" href="${item[0]}"><i class="bi ${item[2]}"></i><span>${item[3]}</span>${item[4] ? `<span class="nav-pill">${item[4]}</span>` : ""}</a>`).join("")}
  `).join("");

  const appShell = (content, actions = "") => {
    const meta = pageMeta[currentPage] || ["Academic StudyPilot", "Your academic success workspace."];
    return `
      ${loader()}
      <div class="app-shell">
        <aside class="app-sidebar" id="appSidebar">
          <a href="index.html">${brand()}</a>
          <nav class="sidebar-nav" aria-label="Primary navigation">${sidebar()}</nav>
          <div class="sidebar-user"><span class="avatar">MR</span><div><strong>Najmul Alam Efti</strong><small>CSE · Semester 6</small></div><i class="bi bi-three-dots ms-auto"></i></div>
        </aside>
        <div class="sidebar-backdrop" id="sidebarBackdrop"></div>
        <div class="app-main">
          <header class="app-topbar">
            <button class="btn btn-icon btn-ghost mobile-menu" id="mobileMenu" type="button" aria-label="Open navigation"><i class="bi bi-list"></i></button>
            <div class="topbar-search"><i class="bi bi-search"></i><input class="form-control" id="globalSearch" type="search" placeholder="Search courses, resources, events…" aria-label="Search"></div>
            <div class="topbar-actions">
              
              <button class="btn btn-icon theme-toggle" type="button" aria-label="Switch color theme"><i class="bi bi-moon-stars"></i></button>
              <button class="btn btn-icon position-relative" id="notificationButton" type="button" aria-label="Notifications"><i class="bi bi-bell"></i><span class="notification-dot"></span></button>
              <a class="topbar-profile" href="profile.html"><span class="avatar">MR</span><div><strong>Najmul Alam Efti</strong><small>Student</small></div></a>
            </div>
          </header>
          <main class="page-content">
            <div class="page-heading"><div><h1>${meta[0]}</h1><p>${meta[1]}</p></div><div class="page-actions">${actions}</div></div>
            ${content}
          </main>
        </div>
      </div>
      <div class="toast-stack" id="toastStack" aria-live="polite"></div>
      <div class="modal fade" id="appModal" tabindex="-1" aria-hidden="true"><div class="modal-dialog modal-dialog-centered modal-lg"><div class="modal-content"><div class="modal-header"><h5 class="modal-title" id="appModalTitle">StudyPilot</h5><button type="button" class="btn-close" data-bs-dismiss="modal" data-close-modal aria-label="Close"></button></div><div class="modal-body" id="appModalBody"></div><div class="modal-footer" id="appModalFooter"><button type="button" class="btn btn-ghost" data-bs-dismiss="modal" data-close-modal>Close</button></div></div></div></div>`;
  };

  const dashboardPage = () => {
    const content = `
      <div class="row g-3 mb-3">
        <div class="col-sm-6 col-xl-3"><div class="metric-card primary"><div class="metric-icon"><i class="bi bi-mortarboard"></i></div><div class="metric-label">Current CGPA</div><div class="metric-value">3.42 <small style="font-size:.8rem;font-weight:700">/ 4.00</small></div><div class="metric-caption"><i class="bi bi-arrow-up-right"></i> +0.08 from last semester</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon"><i class="bi bi-bullseye"></i></div><div class="metric-label">Target CGPA</div><div class="metric-value">3.65</div><div class="metric-caption"><span class="mini-badge primary">0.23 to go</span></div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--secondary);background:rgba(20,184,166,.12)"><i class="bi bi-fire"></i></div><div class="metric-label">Study streak</div><div class="metric-value">11 days</div><div class="metric-caption up"><i class="bi bi-trophy"></i> Personal best this term</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--warning);background:rgba(245,158,11,.12)"><i class="bi bi-clock-history"></i></div><div class="metric-label">Weekly study goal</div><div class="metric-value">14.5h <small style="font-size:.8rem;font-weight:700">/ 18h</small></div><div class="progress mt-3"><div class="progress-bar warning" style="width:81%"></div></div></div></div>
      </div>

      <div class="row g-3 mb-3">
        <div class="col-xl-8"><section class="card-ui h-100"><div class="card-head"><div><h2>Academic performance</h2><p>GPA movement across six semesters</p></div><span class="mini-badge success"><i class="bi bi-graph-up"></i> Improving</span></div><div class="card-body-ui"><div class="chart-wrap"><canvas id="gpaTrendChart" aria-label="GPA trend chart"></canvas></div></div></section></div>
        <div class="col-xl-4"><section class="card-ui h-100"><div class="card-head"><div><h2>Weekly study rhythm</h2><p>Hours completed by day</p></div><button class="btn btn-icon btn-ghost" data-toast="Weekly report ready|Your detailed study report is available in Progress."><i class="bi bi-download"></i></button></div><div class="card-body-ui"><div class="chart-wrap"><canvas id="studyHoursChart" aria-label="Weekly study hours chart"></canvas></div></div></section></div>
      </div>

      <div class="row g-3 mb-3">
        <div class="col-xl-7"><section class="card-ui h-100"><div class="card-head"><div><h2>Upcoming assessments</h2><p>Your next academic deadlines</p></div><a class="btn btn-sm btn-ghost" href="assessments.html">View all</a></div><div class="card-body-ui">
          ${assessmentData.slice(1,5).map((item, index) => `<div class="list-row"><span class="list-icon ${index === 0 ? "danger" : index === 1 ? "warning" : ""}"><i class="bi ${item.icon}"></i></span><div><div class="list-title">${item.title}</div><div class="list-meta">${item.course} · ${item.type}</div></div><div class="list-end"><span class="mini-badge ${item.badge}">${item.status}</span><div class="list-meta mt-1 ${index === 0 ? "deadline-soon" : ""}">${item.deadline}</div></div></div>`).join("")}
        </div></section></div>
        <div class="col-xl-5"><section class="card-ui h-100"><div class="card-head"><div><h2>Course priorities</h2><p>Suggested focus for this week</p></div><a class="btn btn-sm btn-ghost" href="priorities.html">Full analysis</a></div><div class="card-body-ui">
          ${[
            ["Artificial Intelligence", "Critical", "danger", 91, "6h 30m"],
            ["Operating Systems", "High", "warning", 82, "5h 00m"],
            ["Database Systems", "Medium", "primary", 68, "3h 30m"]
          ].map(item => `<div class="list-row"><span class="list-icon ${item[2]}"><i class="bi bi-journal-bookmark"></i></span><div class="flex-grow-1"><div class="d-flex justify-content-between gap-2"><span class="list-title">${item[0]}</span><span class="list-meta">${item[4]}</span></div><div class="progress mt-2"><div class="progress-bar ${item[2]}" style="width:${item[3]}%"></div></div></div><span class="mini-badge ${item[2]}">${item[1]}</span></div>`).join("")}
        </div></section></div>
      </div>

      <section class="card-ui"><div class="card-head"><div><h2>Quick actions</h2><p>Jump back into your most useful workflows</p></div></div><div class="card-body-ui"><div class="row g-3">
        ${[
          ["planner.html#gpa", "bi-calculator", "Calculate GPA", "Test this semester's outcome"],
          ["planner.html#study", "bi-calendar2-plus", "Plan study time", "Build today's study blocks"],
          ["progress.html#weak-topics", "bi-search-heart", "Review weak topics", "3 topics need attention"],
          ["resources.html", "bi-collection-play", "Find resources", "Browse recommended materials"]
        ].map(item => `<div class="col-sm-6 col-xl-3"><a class="quick-action" href="${item[0]}"><i class="bi ${item[1]}"></i><div><strong>${item[2]}</strong><small>${item[3]}</small></div><i class="bi bi-chevron-right ms-auto"></i></a></div>`).join("")}
      </div></div></section>`;
    return appShell(content, `<a class="btn btn-ghost" href="planner.html#study"><i class="bi bi-calendar2-week me-2"></i>Today's plan</a><button class="btn btn-primary" data-toast="Progress synced|Your demo dashboard is already up to date."><i class="bi bi-arrow-repeat me-2"></i>Sync progress</button>`);
  };

  const profilePage = () => {
    const content = `
      <section class="card-ui mb-3 overflow-hidden"><div class="profile-cover"></div><div class="profile-summary"><div class="profile-avatar">MR</div><div><h2>Najul Alam Efti</h2><p>B.Sc. in Computer Science & Engineering · Student ID: ASP-2023-0142</p><div class="mt-2"><span class="mini-badge primary">Active student</span> <span class="mini-badge neutral">Semester 6</span></div></div><div class="profile-complete"><div class="d-flex justify-content-between small mb-2"><strong>Profile completion</strong><span class="text-muted">88%</span></div><div class="progress"><div class="progress-bar primary" style="width:88%"></div></div></div></div></section>
      <div class="row g-3">
        <div class="col-xl-8">
          <section class="card-ui mb-3"><div class="card-head"><div><h2>Personal information</h2><p>Core student and contact details</p></div><button class="btn btn-sm btn-ghost" id="editProfileButton"><i class="bi bi-pencil me-1"></i>Edit</button></div><div class="card-body-ui"><div class="row g-0">
            <div class="col-md-6 pe-md-4"><div class="data-pair"><span>Full name</span><strong>Najmul Alam Efti</strong></div><div class="data-pair"><span>Email address</span><strong>najmul.alam@student.edu</strong></div><div class="data-pair"><span>Phone</span><strong>+880 18•• ••• 5256</strong></div></div>
            <div class="col-md-6 ps-md-4"><div class="data-pair"><span>Date of birth</span><strong>04 May 2002</strong></div><div class="data-pair"><span>Preferred language</span><strong>English · Bangla</strong></div><div class="data-pair"><span>Home city</span><strong>Chattogram, Bangladesh</strong></div></div>
          </div></div></section>
          <section class="card-ui"><div class="card-head"><div><h2>Academic goals</h2><p>Your outcomes for the current academic year</p></div><a class="btn btn-sm btn-ghost" href="planner.html#target">Adjust goal</a></div><div class="card-body-ui">
            <div class="row g-3"><div class="col-md-4"><div class="p-3 rounded-4 bg-surface border border-subtle"><span class="text-muted small">Target CGPA</span><strong class="d-block fs-4 mt-1">3.65</strong><span class="mini-badge primary mt-2">By Semester 8</span></div></div><div class="col-md-4"><div class="p-3 rounded-4 bg-surface border border-subtle"><span class="text-muted small">Weekly study</span><strong class="d-block fs-4 mt-1">18 hours</strong><span class="mini-badge success mt-2">81% this week</span></div></div><div class="col-md-4"><div class="p-3 rounded-4 bg-surface border border-subtle"><span class="text-muted small">Primary focus</span><strong class="d-block fs-6 mt-2">Core CSE courses</strong><span class="mini-badge warning mt-2">2 at risk</span></div></div></div>
          </div></section>
        </div>
        <div class="col-xl-4">
          <section class="card-ui mb-3"><div class="card-head"><div><h2>Academic identity</h2><p>University placement</p></div></div><div class="card-body-ui"><div class="data-pair"><span>Department</span><strong>Computer Science & Engineering</strong></div><div class="data-pair"><span>Program</span><strong>B.Sc. in CSE</strong></div><div class="data-pair"><span>Current semester</span><strong>6th Semester · Spring 2026</strong></div><div class="data-pair"><span>Academic advisor</span><strong>Rashed Sir
          /strong></div><div class="data-pair"><span>Credits completed</span><strong>84 of 148 credits</strong></div></div></section>
          <section class="card-ui"><div class="card-head"><div><h2>Study preferences</h2><p>Used to personalize your plan</p></div></div><div class="card-body-ui"><span class="preference-chip"><i class="bi bi-sunrise"></i> Morning focus</span><span class="preference-chip"><i class="bi bi-hourglass-split"></i> 50-minute blocks</span><span class="preference-chip"><i class="bi bi-volume-mute"></i> Quiet environment</span><span class="preference-chip"><i class="bi bi-calendar3"></i> 6 days/week</span><span class="preference-chip"><i class="bi bi-bell"></i> Break reminders</span><span class="preference-chip"><i class="bi bi-people"></i> Group revision</span></div></section>
        </div>
      </div>`;
    return appShell(content, `<button class="btn btn-primary" id="editProfileTop"><i class="bi bi-pencil-square me-2"></i>Edit profile</button>`);
  };

  const coursesPage = () => {
    const content = `
      <div class="row g-3 mb-3">
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon"><i class="bi bi-journals"></i></div><div class="metric-label">Active courses</div><div class="metric-value">6</div><div class="metric-caption">16.5 registered credits</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--secondary);background:rgba(20,184,166,.12)"><i class="bi bi-check2-circle"></i></div><div class="metric-label">Average progress</div><div class="metric-value">66.5%</div><div class="metric-caption up"><i class="bi bi-arrow-up"></i> 7% this month</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--warning);background:rgba(245,158,11,.12)"><i class="bi bi-clock"></i></div><div class="metric-label">Study planned</div><div class="metric-value">18h</div><div class="metric-caption">For the current week</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--danger);background:rgba(239,68,68,.11)"><i class="bi bi-exclamation-triangle"></i></div><div class="metric-label">Needs attention</div><div class="metric-value">2</div><div class="metric-caption warn">AI and Operating Systems</div></div></div>
      </div>
      <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-3"><div class="filter-pills" id="courseFilters"><button class="filter-btn active" data-filter="all">All courses</button><button class="filter-btn" data-filter="risk">Needs focus</button><button class="filter-btn" data-filter="strong">Strong progress</button></div><div class="d-flex gap-2"><select class="form-select form-select-sm" aria-label="Sort courses"><option>Sort: Priority</option><option>Progress</option><option>Course code</option></select></div></div>
      <div class="row g-3" id="courseGrid">
        ${courseData.map(course => `<div class="col-md-6 col-xl-4 course-item" data-state="${course.progress < 60 ? "risk" : course.progress >= 75 ? "strong" : "normal"}"><article class="card-ui hoverable course-card"><div class="course-band ${course.color}"></div><div class="course-body"><div class="d-flex justify-content-between gap-2"><span class="course-code">${course.code}</span><span class="mini-badge neutral">${course.credits} credits</span></div><h3>${course.title}</h3><div class="course-faculty"><span class="avatar ${course.color === "teal" ? "teal" : ""}">${course.initials}</span><span>${course.faculty}</span></div><div class="d-flex justify-content-between small mb-2"><span class="text-muted">Course progress</span><strong>${course.progress}%</strong></div><div class="progress"><div class="progress-bar ${course.progress < 60 ? "danger" : course.progress >= 75 ? "success" : "primary"}" style="width:${course.progress}%"></div></div><div class="course-footer"><span><i class="bi bi-clock me-1"></i>${course.progress < 60 ? "5h planned" : "3.5h planned"}</span><button class="btn btn-sm btn-soft-primary" data-toast="Course workspace|${course.title} workspace is ready in the full product.">Open course</button></div></div></article></div>`).join("")}
      </div>
      <div class="empty-state" id="courseEmpty"><div class="empty-icon"><i class="bi bi-journals"></i></div><h3 class="h5">No courses match this filter</h3><p class="text-muted">Try a different progress category.</p></div>`;
    return appShell(content, `<button class="btn btn-ghost" data-toast="Semester report prepared|A course summary would be exported in the production version."><i class="bi bi-download me-2"></i>Export summary</button><button class="btn btn-primary" data-toast="Course request saved|The demo request has been added for department review."><i class="bi bi-plus-lg me-2"></i>Request course</button>`);
  };

  const assessmentsPage = () => {
    const content = `
      <div class="row g-3 mb-3">
        <div class="col-6 col-xl-3"><div class="metric-card"><div class="metric-icon"><i class="bi bi-calendar2-check"></i></div><div class="metric-label">Total assessments</div><div class="metric-value">18</div><div class="metric-caption">This semester</div></div></div>
        <div class="col-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--success);background:rgba(34,197,94,.12)"><i class="bi bi-check-circle"></i></div><div class="metric-label">Completed</div><div class="metric-value">11</div><div class="metric-caption up">61% complete</div></div></div>
        <div class="col-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--warning);background:rgba(245,158,11,.12)"><i class="bi bi-hourglass-split"></i></div><div class="metric-label">Upcoming</div><div class="metric-value">6</div><div class="metric-caption">Next 30 days</div></div></div>
        <div class="col-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--danger);background:rgba(239,68,68,.11)"><i class="bi bi-alarm"></i></div><div class="metric-label">Due this week</div><div class="metric-value">3</div><div class="metric-caption warn">One due in 2 days</div></div></div>
      </div>
      <div class="d-flex flex-wrap justify-content-between gap-3 mb-3"><div class="filter-pills" id="assessmentFilters">${["All", "Quiz", "CT", "Assignment", "Midterm", "Final"].map((type,index) => `<button class="filter-btn ${index === 0 ? "active" : ""}" data-filter="${type.toLowerCase()}">${type}</button>`).join("")}</div><select class="form-select form-select-sm" style="width:auto" id="assessmentStatus"><option value="all">All status</option><option value="graded">Graded</option><option value="upcoming">Upcoming</option><option value="in progress">In progress</option></select></div>
      <section class="card-ui overflow-hidden"><div class="card-head pb-3"><div><h2>Assessment timeline</h2><p>Marks, deadlines, and submission status</p></div><span class="mini-badge primary"><i class="bi bi-calendar3"></i> Spring 2026</span></div><div class="table-responsive"><table class="table align-middle"><thead><tr><th>Assessment</th><th>Course</th><th>Marks</th><th>Deadline</th><th>Status</th><th></th></tr></thead><tbody id="assessmentTable">
        ${assessmentData.map(item => `<tr data-type="${item.type.toLowerCase()}" data-status="${item.status.toLowerCase()}"><td><div class="assessment-type"><span class="list-icon"><i class="bi ${item.icon}"></i></span><div><strong class="d-block">${item.title}</strong><span class="text-muted small">${item.type}</span></div></div></td><td>${item.course}</td><td><strong>${item.marks}</strong></td><td class="${item.status === "Due soon" ? "deadline-soon" : ""}">${item.deadline}</td><td><span class="mini-badge ${item.badge}">${item.status}</span></td><td><button class="btn btn-sm btn-ghost" data-toast="Assessment details|${item.title} details opened in demo mode."><i class="bi bi-chevron-right"></i></button></td></tr>`).join("")}
      </tbody></table><div class="empty-state" id="assessmentEmpty"><div class="empty-icon"><i class="bi bi-clipboard2-check"></i></div><h3 class="h5">Nothing in this view</h3><p class="text-muted">Change the assessment type or status filter.</p></div></div></section>`;
    return appShell(content, `<button class="btn btn-ghost" data-toast="Calendar connected|Assessment deadlines are ready to sync in the full product."><i class="bi bi-calendar-plus me-2"></i>Sync calendar</button><button class="btn btn-primary" data-toast="Reminder created|We will remind you about Process Scheduling CT tomorrow morning."><i class="bi bi-bell me-2"></i>Set reminder</button>`);
  };

  const gradeOptions = (selected) => [[4,"A+"],[3.75,"A"],[3.5,"A-"],[3.25,"B+"],[3,"B"],[2.75,"B-"],[2.5,"C+"],[2.25,"C"],[2,"D"],[0,"F"]].map(item => `<option value="${item[0]}" ${Number(selected) === item[0] ? "selected" : ""}>${item[1]} · ${item[0].toFixed(2)}</option>`).join("");

  const plannerPage = () => {
    const content = `
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
        <div class="tab-switcher" role="tablist" aria-label="Planner tools"><button class="active" data-planner-tab="gpa"><i class="bi bi-calculator me-1"></i>GPA calculator</button><button data-planner-tab="cgpa"><i class="bi bi-graph-up me-1"></i>CGPA planner</button><button data-planner-tab="study"><i class="bi bi-calendar2-week me-1"></i>Smart study plan</button></div>
        <span class="mini-badge success"><i class="bi bi-shield-check"></i> Calculations use the 4.00 scale</span>
      </div>

      <section class="planner-panel active" data-planner-panel="gpa" id="gpa">
        <div class="row g-3"><div class="col-xl-8"><div class="card-ui"><div class="card-head"><div><h2>Semester GPA calculator</h2><p>Add courses, credits, and expected letter grades</p></div><button class="btn btn-sm btn-ghost" id="addGpaCourse"><i class="bi bi-plus-lg me-1"></i>Add course</button></div><div class="card-body-ui"><div id="gpaRows">
          ${[
            ["Database Management Systems",3,3.75], ["Operating Systems",3,3.25], ["Computer Networks",3,3.5], ["Software Engineering",3,3.75], ["Artificial Intelligence",3,3.0], ["Database Systems Lab",1.5,4]
          ].map((row,index) => `<div class="course-input-row gpa-course-row"><div class="course-name"><label class="form-label">Course ${index+1}</label><input class="form-control" value="${row[0]}" aria-label="Course name"></div><div><label class="form-label">Credits</label><input class="form-control gpa-credit" type="number" min=".5" max="6" step=".5" value="${row[1]}"></div><div><label class="form-label">Expected grade</label><select class="form-select gpa-grade">${gradeOptions(row[2])}</select></div><button class="btn btn-icon btn-ghost remove-gpa-row" type="button" aria-label="Remove course"><i class="bi bi-trash3"></i></button></div>`).join("")}
        </div><button class="btn btn-primary mt-2" id="calculateGpa"><i class="bi bi-calculator me-2"></i>Calculate semester GPA</button></div></div></div><div class="col-xl-4"><div class="calculator-result h-100"><span class="d-block opacity-75 small fw-bold text-uppercase">Projected semester GPA</span><div class="result-number mt-3" id="gpaResult">3.50</div><p class="opacity-75 mt-3">Based on <strong id="gpaCredits">16.5</strong> attempted credits.</p><div class="target-meter"><span id="gpaMeter" style="width:87.5%"></span></div><div class="d-flex justify-content-between small opacity-75"><span>Current projection</span><span id="gpaMessage">Good standing</span></div><hr class="border-white border-opacity-25 my-4"><div class="small opacity-75"><i class="bi bi-lightbulb me-2"></i>An A- in Artificial Intelligence would raise this projection by approximately 0.09.</div></div></div></div>
      </section>

      <section class="planner-panel" data-planner-panel="cgpa" id="target">
        <div class="row g-3"><div class="col-xl-7"><div class="card-ui"><div class="card-head"><div><h2>Target CGPA planner</h2><p>Find the average GPA needed across your remaining credits</p></div></div><div class="card-body-ui"><div class="row g-3"><div class="col-sm-6"><label class="form-label">Current CGPA</label><input class="form-control" id="currentCgpa" type="number" step=".01" min="0" max="4" value="3.42"></div><div class="col-sm-6"><label class="form-label">Completed credits</label><input class="form-control" id="completedCredits" type="number" min="0" value="84"></div><div class="col-sm-6"><label class="form-label">Target CGPA</label><input class="form-control" id="targetCgpa" type="number" step=".01" min="0" max="4" value="3.65"></div><div class="col-sm-6"><label class="form-label">Remaining credits</label><input class="form-control" id="remainingCredits" type="number" min="1" value="64"></div></div><button class="btn btn-primary mt-4" id="calculateTarget"><i class="bi bi-bullseye me-2"></i>Build target scenario</button></div></div></div><div class="col-xl-5"><div class="calculator-result h-100"><span class="d-block opacity-75 small fw-bold text-uppercase">Required average GPA</span><div class="result-number mt-3" id="requiredGpa">3.95</div><p class="opacity-75 mt-3" id="targetNarrative">Across your remaining 64 credits to reach a final CGPA of 3.65.</p><div class="target-meter"><span id="targetMeter" style="width:98.7%"></span></div><div class="d-flex justify-content-between small opacity-75"><span>Challenge level</span><span id="targetLevel">Ambitious</span></div><hr class="border-white border-opacity-25 my-4"><div class="small opacity-75" id="targetTip"><i class="bi bi-stars me-2"></i>Prioritize high-credit courses and protect your lab grades.</div></div></div></div>
      </section>

      <section class="planner-panel" data-planner-panel="study" id="study">
        <div class="row g-3"><div class="col-xl-8"><section class="card-ui"><div class="card-head"><div><h2>Weekly study calendar</h2><p>Balanced around your classes and energy preferences</p></div><button class="btn btn-sm btn-ghost" id="optimizePlan"><i class="bi bi-stars me-1"></i>Optimize</button></div><div class="card-body-ui"><div class="week-strip mb-4">${[["Sun",16],["Mon",17],["Tue",18],["Wed",19],["Thu",20],["Fri",21],["Sat",22]].map((day,index)=>`<button class="day-pill ${index===0?"active":""}" data-day="${day[0]}"><small>${day[0]}</small><strong>${day[1]}</strong></button>`).join("")}</div><div class="timeline" id="studyTimeline">
          <div class="study-block"><div class="study-time">8:00–8:50</div><span class="list-icon"><i class="bi bi-cpu"></i></span><div><strong>AI search strategies</strong><small>Focused study · Chapter 3 · 50 min</small></div><span class="mini-badge danger ms-auto">Critical</span></div>
          <div class="study-block break"><div class="study-time">8:50–9:05</div><span class="list-icon teal"><i class="bi bi-cup-hot"></i></span><div><strong>Mindful break</strong><small>Hydrate and leave your desk · 15 min</small></div></div>
          <div class="study-block"><div class="study-time">10:30–11:20</div><span class="list-icon warning"><i class="bi bi-pc-display"></i></span><div><strong>Process scheduling problems</strong><small>Practice set for upcoming CT · 50 min</small></div><span class="mini-badge warning ms-auto">High</span></div>
          <div class="study-block revision"><div class="study-time">4:30–5:10</div><span class="list-icon warning"><i class="bi bi-arrow-repeat"></i></span><div><strong>Database normalization revision</strong><small>Active recall + 10 practice questions · 40 min</small></div></div>
          <div class="study-block"><div class="study-time">8:00–8:50</div><span class="list-icon"><i class="bi bi-diagram-3"></i></span><div><strong>Network design assignment</strong><small>Complete topology and cost section · 50 min</small></div><span class="mini-badge primary ms-auto">Due Aug 21</span></div>
        </div></div></section></div><div class="col-xl-4"><section class="card-ui mb-3"><div class="card-head"><div><h2>Today's balance</h2><p>Sunday, August 16</p></div></div><div class="card-body-ui"><div class="d-flex align-items-center gap-4"><div class="ring-progress" style="--value:72%"><strong>72%</strong></div><div><strong class="d-block fs-5">3h 20m</strong><span class="text-muted small">of 4h 35m planned</span><div class="mt-2"><span class="mini-badge success">Healthy load</span></div></div></div></div></section><section class="card-ui"><div class="card-head"><div><h2>Plan ingredients</h2><p>What today's plan protects</p></div></div><div class="card-body-ui"><div class="list-row"><span class="list-icon"><i class="bi bi-book"></i></span><div><div class="list-title">Focused study</div><div class="list-meta">3 blocks · 2h 30m</div></div></div><div class="list-row"><span class="list-icon warning"><i class="bi bi-arrow-repeat"></i></span><div><div class="list-title">Revision</div><div class="list-meta">1 block · 40m</div></div></div><div class="list-row"><span class="list-icon teal"><i class="bi bi-cup-hot"></i></span><div><div class="list-title">Break reminders</div><div class="list-meta">4 reminders · 45m</div></div></div></div></section></div></div>
      </section>`;
    return appShell(content, `<button class="btn btn-ghost" data-toast="Plan saved as PDF|A printable study plan would download in the production version."><i class="bi bi-download me-2"></i>Export plan</button><button class="btn btn-primary" id="savePlan"><i class="bi bi-check2-circle me-2"></i>Save scenario</button>`);
  };

  const prioritiesPage = () => {
    const priorities = [
      {level:"critical",badge:"danger",label:"Critical",score:91,course:"Artificial Intelligence",code:"CSE 3209",reason:"Your topic confidence is low (46%) and the final carries 60% of the course grade.",time:"6h 30m",action:"Complete search-strategy recovery path"},
      {level:"high",badge:"warning",label:"High",score:82,course:"Operating Systems",code:"CSE 3203",reason:"A 20-mark CT is due in 2 days and scheduling practice accuracy is currently 58%.",time:"5h 00m",action:"Solve 3 scheduling problem sets"},
      {level:"high",badge:"warning",label:"High",score:77,course:"Computer Networks",code:"CSE 3205",reason:"The campus network assignment is due this week and still has two incomplete sections.",time:"4h 15m",action:"Finish topology and security sections"},
      {level:"medium",badge:"primary",label:"Medium",score:68,course:"Database Management Systems",code:"CSE 3201",reason:"Performance is stable, but normalization mistakes appeared in the latest quiz review.",time:"3h 30m",action:"Revise 2NF, 3NF, and BCNF"},
      {level:"medium",badge:"primary",label:"Medium",score:61,course:"Software Engineering",code:"CSE 3207",reason:"Strong current score with a moderate revision gap before the next assessment.",time:"2h 30m",action:"Review testing and quality assurance"}
    ];
    const content = `
      <section class="card-ui mb-3"><div class="card-body-ui"><div class="row align-items-center g-4"><div class="col-lg-7"><span class="eyebrow"><i class="bi bi-stars"></i> Priority engine explanation</span><h2 class="mt-3 mb-2 fw-bold">Your next best academic move is clear.</h2><p class="text-muted mb-0">Scores combine deadline urgency, assessment weight, current marks, course progress, topic confidence, and the time needed to recover.</p></div><div class="col-lg-5"><div class="row g-2"><div class="col-4 text-center"><strong class="d-block fs-3 text-danger">1</strong><span class="text-muted small">Critical</span></div><div class="col-4 text-center"><strong class="d-block fs-3 text-warning">2</strong><span class="text-muted small">High</span></div><div class="col-4 text-center"><strong class="d-block fs-3 text-primary">2</strong><span class="text-muted small">Medium</span></div></div></div></div></div></section>
      <div class="d-flex align-items-center justify-content-between mb-3"><div class="filter-pills" id="priorityFilters"><button class="filter-btn active" data-filter="all">All priorities</button><button class="filter-btn" data-filter="critical">Critical</button><button class="filter-btn" data-filter="high">High</button><button class="filter-btn" data-filter="medium">Medium</button></div><span class="text-muted small d-none d-md-inline">Updated today at 2:30 PM</span></div>
      <div class="row g-3" id="priorityGrid">${priorities.map(item => `<div class="col-xl-6 priority-item" data-level="${item.level}"><article class="card-ui hoverable priority-card ${item.level} h-100"><div class="d-flex gap-3 align-items-start"><div class="priority-score">${item.score}</div><div class="flex-grow-1"><div class="d-flex justify-content-between gap-2 flex-wrap"><div><span class="course-code">${item.code}</span><h3 class="h6 fw-bold mt-1 mb-0">${item.course}</h3></div><span class="mini-badge ${item.badge}">${item.label} priority</span></div><div class="reason-box"><i class="bi bi-lightbulb me-1"></i>${item.reason}</div><div class="d-flex justify-content-between align-items-center gap-3 mt-3"><div><span class="text-muted small d-block">Suggested study time</span><strong>${item.time} this week</strong></div><button class="btn btn-sm btn-soft-primary" data-toast="Added to study plan|${item.action} has been added to today's plan.">Add to plan</button></div></div></div></article></div>`).join("")}</div>`;
    return appShell(content, `<button class="btn btn-ghost" data-toast="Priority model explained|The demo score uses deadlines, marks, progress, and topic confidence."><i class="bi bi-info-circle me-2"></i>How scoring works</button><a class="btn btn-primary" href="planner.html#study"><i class="bi bi-calendar2-plus me-2"></i>Build study plan</a>`);
  };

  const progressPage = () => {
    const content = `
      <div class="row g-3 mb-3">
        <div class="col-sm-6 col-xl-3"><div class="metric-card primary"><div class="metric-icon"><i class="bi bi-lightning-charge"></i></div><div class="metric-label">Productivity score</div><div class="metric-value">86%</div><div class="metric-caption"><i class="bi bi-arrow-up"></i> +8% from last week</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--secondary);background:rgba(20,184,166,.12)"><i class="bi bi-clock-history"></i></div><div class="metric-label">Study hours</div><div class="metric-value">14.5h</div><div class="metric-caption up">81% of weekly target</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--warning);background:rgba(245,158,11,.12)"><i class="bi bi-fire"></i></div><div class="metric-label">Current streak</div><div class="metric-value">11 days</div><div class="metric-caption">Best: 11 days</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:#8b5cf6;background:rgba(139,92,246,.12)"><i class="bi bi-award"></i></div><div class="metric-label">Achievements</div><div class="metric-value">7</div><div class="metric-caption">2 earned this month</div></div></div>
      </div>
      <div class="row g-3 mb-3">
        <div class="col-xl-7"><section class="card-ui h-100"><div class="card-head"><div><h2>Study hours</h2><p>Focused time across the last eight weeks</p></div><span class="mini-badge success">+12% trend</span></div><div class="card-body-ui"><div class="chart-wrap"><canvas id="progressHoursChart"></canvas></div></div></section></div>
        <div class="col-xl-5"><section class="card-ui h-100"><div class="card-head"><div><h2>Topic completion</h2><p>Mastery across active courses</p></div></div><div class="card-body-ui">${[["Database Systems",78,"success"],["Software Engineering",82,"success"],["Computer Networks",71,"primary"],["Operating Systems",58,"warning"],["Artificial Intelligence",46,"danger"]].map(item=>`<div class="mb-3"><div class="d-flex justify-content-between small mb-2"><strong>${item[0]}</strong><span class="text-muted">${item[1]}%</span></div><div class="progress"><div class="progress-bar ${item[2]}" style="width:${item[1]}%"></div></div></div>`).join("")}</div></section></div>
      </div>
      <section class="card-ui mb-3"><div class="card-head"><div><h2>Achievements</h2><p>Milestones that reward consistent academic habits</p></div><button class="btn btn-sm btn-ghost" data-toast="Achievement gallery|You have unlocked 7 of 18 achievements.">View all</button></div><div class="card-body-ui"><div class="row g-3">
        ${[["bi-fire","Consistency champion","Study 10 days in a row","Unlocked"],["bi-clock-history","Deep work","Complete 10 focused hours","Unlocked"],["bi-journal-check","Topic finisher","Master 25 course topics","Unlocked"],["bi-trophy","Perfect week","Complete every weekly plan item","2 items left"]].map((item,index)=>`<div class="col-sm-6 col-xl-3"><div class="achievement ${index===3?"locked":""}"><span class="achievement-icon"><i class="bi ${item[0]}"></i></span><div><strong class="d-block small">${item[1]}</strong><span class="text-muted" style="font-size:.68rem">${item[2]}</span><span class="mini-badge ${index===3?"neutral":"success"} mt-2">${item[3]}</span></div></div></div>`).join("")}
      </div></div></section>
      <section id="weak-topics"><div class="d-flex flex-wrap align-items-end justify-content-between gap-2 mb-3"><div><h2 class="h4 fw-bold mb-1">Weak-topic detection</h2><p class="text-muted mb-0">Evidence-based areas where focused recovery can improve your results.</p></div><span class="mini-badge neutral"><i class="bi bi-arrow-repeat"></i> Updated after every assessment</span></div><div class="row g-3">
        ${[
          ["Uninformed Search Strategies","Artificial Intelligence",42,"5 / 5","Review BFS, DFS, UCS comparisons","Complete 12 guided practice problems"],
          ["CPU Scheduling Algorithms","Operating Systems",54,"4 / 5","Revisit waiting and turnaround time","Practice FCFS, SJF, and Round Robin"],
          ["Database Normalization","Database Management Systems",63,"3 / 5","Review functional dependencies","Convert 3 schemas through BCNF"]
        ].map((item,index)=>`<div class="col-lg-4"><article class="card-ui hoverable weak-card h-100"><div class="d-flex justify-content-between align-items-start gap-2"><span class="list-icon ${index===0?"danger":"warning"}"><i class="bi bi-search-heart"></i></span><span class="mini-badge ${index===0?"danger":"warning"}">${item[2]}% mastery</span></div><h3 class="h6 fw-bold mt-3 mb-1">${item[0]}</h3><p class="text-muted small">${item[1]}</p><div class="confidence"><span class="text-muted small">Confidence</span><div class="confidence-dots">${Array.from({length:5},(_,i)=>`<span class="${i < Number(item[3][0]) ? "on" : ""}"></span>`).join("")}</div><span class="text-muted small">${item[3]}</span></div><ul class="suggestion-list"><li><i class="bi bi-check2-circle"></i>${item[4]}</li><li><i class="bi bi-check2-circle"></i>${item[5]}</li></ul><button class="btn btn-soft-primary w-100 mt-2" data-toast="Recovery path started|${item[0]} has been added to your smart study plan.">Start recovery path</button></article></div>`).join("")}
      </div></section>`;
    return appShell(content, `<button class="btn btn-ghost" data-toast="Progress report prepared|Your demo progress summary is ready."><i class="bi bi-download me-2"></i>Export report</button><a class="btn btn-primary" href="planner.html#study"><i class="bi bi-calendar2-check me-2"></i>Plan recovery</a>`);
  };

  const resourcesPage = () => {
    const content = `
      <section class="card-ui mb-3"><div class="card-body-ui"><div class="row g-3 align-items-end"><div class="col-lg-6"><label class="form-label">Search learning resources</label><div class="position-relative"><i class="bi bi-search position-absolute text-muted" style="left:14px;top:14px"></i><input class="form-control ps-5" id="resourceSearch" type="search" placeholder="Search by title, course, or author…"></div></div><div class="col-sm-6 col-lg-3"><label class="form-label">Course</label><select class="form-select" id="resourceCourse"><option value="all">All courses</option>${courseData.slice(0,5).map(course=>`<option value="${course.title.toLowerCase()}">${course.title}</option>`).join("")}</select></div><div class="col-sm-6 col-lg-3"><label class="form-label">Sort by</label><select class="form-select"><option>Recommended</option><option>Most saved</option><option>Recently added</option></select></div></div></div></section>
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3"><div class="filter-pills" id="resourceFilters"><button class="filter-btn active" data-filter="all">All resources</button><button class="filter-btn" data-filter="pdf"><i class="bi bi-file-earmark-pdf me-1"></i>PDFs</button><button class="filter-btn" data-filter="video"><i class="bi bi-play-circle me-1"></i>Videos</button><button class="filter-btn" data-filter="note"><i class="bi bi-journal-richtext me-1"></i>Notes</button></div><span class="text-muted small"><strong id="resourceCount">6</strong> resources found</span></div>
      <div class="row g-3" id="resourceGrid">${resourcesData.map(item=>`<div class="col-md-6 col-xl-4 resource-item" data-type="${item.type}" data-search="${(item.title+" "+item.course+" "+item.author).toLowerCase()}" data-course="${item.course.toLowerCase()}"><article class="card-ui hoverable resource-card"><div class="resource-cover ${item.type}"><i class="bi ${item.icon}"></i><span class="mini-badge position-absolute top-0 start-0 m-3" style="background:rgba(255,255,255,.18);color:#fff">${item.label}</span></div><div class="resource-body"><span class="course-code">${item.course}</span><h3>${item.title}</h3><div class="resource-meta"><span><i class="bi bi-person me-1"></i>${item.author}</span></div><div class="resource-meta mt-2"><span><i class="bi bi-info-circle me-1"></i>${item.meta}</span></div><div class="resource-actions"><button class="btn btn-sm btn-soft-primary" data-toast="Resource opened|${item.title} is ready in demo mode.">${item.type==="video"?"Watch":"Open"} resource</button><button class="btn btn-icon btn-ghost save-resource ${item.saved?"text-primary":""}" type="button" aria-label="Save resource"><i class="bi ${item.saved?"bi-bookmark-fill":"bi-bookmark"}"></i></button></div></div></article></div>`).join("")}</div>
      <div class="empty-state card-ui" id="resourceEmpty"><div class="empty-icon"><i class="bi bi-search"></i></div><h3 class="h5">No matching resources</h3><p class="text-muted">Try a broader keyword or clear one of your filters.</p><button class="btn btn-soft-primary" id="clearResourceFilters">Clear filters</button></div>`;
    return appShell(content, `<button class="btn btn-ghost" data-toast="Saved resources|You have 3 saved learning resources in this demo."><i class="bi bi-bookmark me-2"></i>Saved</button><button class="btn btn-primary" id="suggestResource"><i class="bi bi-plus-lg me-2"></i>Suggest resource</button>`);
  };

  const communityPage = () => {
    const content = `
      <div class="row g-3"><div class="col-xl-8">
        <section class="card-ui mb-3"><div class="card-body-ui"><div class="composer"><span class="avatar">MR</span><div class="flex-grow-1"><textarea class="form-control" id="communityComposer" placeholder="Share a question, insight, or study update…"></textarea><div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mt-3"><div><button class="btn btn-sm btn-ghost" data-toast="Attachment option|File attachment is shown as a prototype control."><i class="bi bi-paperclip"></i></button> <button class="btn btn-sm btn-ghost" data-toast="Course tag|Choose a course tag in the production version."><i class="bi bi-tag"></i></button></div><button class="btn btn-primary btn-sm" id="publishPost">Publish post</button></div></div></div></div></section>
        <div class="filter-pills mb-3"><button class="filter-btn active">For you</button><button class="filter-btn">Questions</button><button class="filter-btn">Resources</button><button class="filter-btn">Study updates</button></div>
        <div id="communityFeed">
          <article class="card-ui post-card mb-3"><div class="post-header"><span class="avatar teal">NR</span><div><strong>Nafisa Rahman</strong><small>Computer Networks · 18 minutes ago</small></div><button class="btn btn-icon btn-ghost ms-auto"><i class="bi bi-three-dots"></i></button></div><h3 class="post-title">Can someone explain why we choose OSPF over RIP for the campus network assignment?</h3><p class="post-copy">I understand the hop-count limitation, but I am unsure how to explain convergence and scalability in the design justification. A short practical example would help.</p><span class="mini-badge primary">Question</span><div class="post-actions"><button class="post-action like-button"><i class="bi bi-hand-thumbs-up me-1"></i><span>24</span></button><button class="post-action"><i class="bi bi-chat-left-text me-1"></i>8 comments</button><button class="post-action ms-auto" data-toast="Post saved|This discussion is now in your saved items."><i class="bi bi-bookmark me-1"></i>Save</button></div></article>
          <article class="card-ui post-card mb-3"><div class="post-header"><span class="avatar orange">SA</span><div><strong>Sakib Ahmed</strong><small>Artificial Intelligence · 1 hour ago</small></div><button class="btn btn-icon btn-ghost ms-auto"><i class="bi bi-three-dots"></i></button></div><h3 class="post-title">BFS vs UCS: one-page comparison sheet</h3><p class="post-copy">I turned today's class discussion into a compact comparison of completeness, optimality, time complexity, and memory. Sharing it before the revision session tonight.</p><div class="p-3 rounded-4 mt-3" style="background:var(--primary-soft);border:1px solid rgba(37,99,235,.15)"><i class="bi bi-file-earmark-pdf text-primary me-2"></i><strong class="small">Search_Strategies_Revision.pdf</strong><span class="text-muted small ms-2">1.4 MB</span></div><div class="post-actions"><button class="post-action like-button"><i class="bi bi-hand-thumbs-up me-1"></i><span>41</span></button><button class="post-action"><i class="bi bi-chat-left-text me-1"></i>12 comments</button><button class="post-action ms-auto" data-toast="Resource saved|The comparison sheet was saved to your resources."><i class="bi bi-bookmark me-1"></i>Save</button></div></article>
          <article class="card-ui post-card"><div class="post-header"><span class="avatar">TM</span><div><strong>Tahmina Morshed</strong><small>Study update · Yesterday</small></div><button class="btn btn-icon btn-ghost ms-auto"><i class="bi bi-three-dots"></i></button></div><h3 class="post-title">Finished my first 10-day study streak 🎉</h3><p class="post-copy">The 50-minute blocks and break reminders made this much more sustainable than my old late-night schedule. Starting a small accountability group for anyone working toward finals.</p><div class="post-actions"><button class="post-action like-button liked"><i class="bi bi-hand-thumbs-up-fill me-1"></i><span>67</span></button><button class="post-action"><i class="bi bi-chat-left-text me-1"></i>19 comments</button><button class="post-action ms-auto" data-toast="Post saved|This study update is now saved."><i class="bi bi-bookmark me-1"></i>Save</button></div></article>
        </div>
      </div><div class="col-xl-4">
        <section class="card-ui mb-3"><div class="card-head"><div><h2>My study groups</h2><p>Small groups, shared momentum</p></div><button class="btn btn-sm btn-ghost" data-toast="Group discovery|More academic groups are available in the full platform.">Discover</button></div><div class="card-body-ui d-grid gap-2">${[["bi-cpu","AI Final Sprint","8 members · Active now"],["bi-database","DBMS Problem Solvers","14 members · 6 new posts"],["bi-alarm","6 AM Focus Club","21 members · Next session tomorrow"]].map(item=>`<div class="group-card"><span class="group-icon"><i class="bi ${item[0]}"></i></span><div><strong>${item[1]}</strong><small>${item[2]}</small></div><button class="btn btn-sm btn-ghost ms-auto group-button">Open</button></div>`).join("")}</div></section>
        <section class="card-ui"><div class="card-head"><div><h2>Popular this week</h2><p>Topics students are discussing</p></div></div><div class="card-body-ui"><div class="list-row"><span class="list-icon"><i class="bi bi-hash"></i></span><div><div class="list-title">final-preparation</div><div class="list-meta">142 posts this week</div></div></div><div class="list-row"><span class="list-icon teal"><i class="bi bi-hash"></i></span><div><div class="list-title">database-normalization</div><div class="list-meta">89 posts this week</div></div></div><div class="list-row"><span class="list-icon warning"><i class="bi bi-hash"></i></span><div><div class="list-title">campus-tech-fest</div><div class="list-meta">73 posts this week</div></div></div></div></section>
      </div></div>`;
    return appShell(content, `<button class="btn btn-ghost" data-toast="Community guidelines|Be respectful, cite sources, and protect student privacy."><i class="bi bi-shield-check me-2"></i>Guidelines</button><button class="btn btn-primary" data-toast="Study group created|Your draft group has been created in demo mode."><i class="bi bi-people me-2"></i>Create group</button>`);
  };

  const facultyPage = () => {
    const content = `
      <section class="faculty-hero mb-3"><div class="row align-items-center g-3"><div class="col-lg-8"><span class="mini-badge" style="background:rgba(255,255,255,.1);color:#bfdbfe">Faculty workspace</span><h2 class="mt-3">Good afternoon, Dr. Farhana.</h2><p>Six students in Database Management Systems may benefit from early academic support this week.</p></div><div class="col-lg-4 text-lg-end"><button class="btn btn-light" data-toast="Intervention list prepared|Six at-risk students were added to the faculty follow-up list."><i class="bi bi-person-check me-2"></i>Review support list</button></div></div></section>
      <div class="row g-3 mb-3">
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon"><i class="bi bi-people"></i></div><div class="metric-label">Active students</div><div class="metric-value">126</div><div class="metric-caption">Across 3 course sections</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--success);background:rgba(34,197,94,.12)"><i class="bi bi-bar-chart"></i></div><div class="metric-label">Average performance</div><div class="metric-value">74.8%</div><div class="metric-caption up">+3.2% vs last term</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--warning);background:rgba(245,158,11,.12)"><i class="bi bi-exclamation-circle"></i></div><div class="metric-label">Support needed</div><div class="metric-value">9</div><div class="metric-caption warn">6 newly identified</div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="metric-card"><div class="metric-icon" style="color:var(--secondary);background:rgba(20,184,166,.12)"><i class="bi bi-check2-all"></i></div><div class="metric-label">Topic coverage</div><div class="metric-value">68%</div><div class="metric-caption">8 of 12 modules</div></div></div>
      </div>
      <div class="row g-3 mb-3"><div class="col-xl-7"><section class="card-ui h-100"><div class="card-head"><div><h2>Course analytics</h2><p>Average assessment performance by module</p></div><select class="form-select form-select-sm" style="width:auto"><option>Database Systems</option><option>Database Lab</option><option>Data Structures</option></select></div><div class="card-body-ui"><div class="chart-wrap"><canvas id="facultyPerformanceChart"></canvas></div></div></section></div><div class="col-xl-5"><section class="card-ui h-100"><div class="card-head"><div><h2>Weak-topic analytics</h2><p>Share of students below 60% mastery</p></div></div><div class="card-body-ui"><div class="chart-wrap"><canvas id="facultyWeakChart"></canvas></div></div></section></div></div>
      <section class="card-ui"><div class="card-head"><div><h2>Student progress overview</h2><p>Students requiring attention based on recent evidence</p></div><button class="btn btn-sm btn-ghost" data-toast="Student list exported|The faculty progress overview is ready for download."><i class="bi bi-download me-1"></i>Export</button></div><div class="card-body-ui">
        <div class="student-row text-muted small fw-bold"><span>Student</span><span>Average</span><span>Engagement</span><span>Status</span></div>
        ${[["AR","Afsana Rahman","58%","Low","At risk","danger"],["MI","Mahmud Islam","62%","Moderate","Watch","warning"],["SK","Sadia Karim","67%","Low","Watch","warning"],["RN","Rafi Nayeem","84%","High","On track","success"],["TS","Tanvir Saha","79%","High","On track","success"]].map((s,index)=>`<div class="student-row"><div class="student-name"><span class="avatar ${index%2?"teal":""}">${s[0]}</span><div><strong class="d-block small">${s[1]}</strong><span class="text-muted" style="font-size:.68rem">CSE-${2023142+index}</span></div></div><strong>${s[2]}</strong><span class="text-muted small">${s[3]}</span><div class="d-flex align-items-center gap-2"><span class="mini-badge ${s[5]}">${s[4]}</span><button class="btn btn-sm btn-ghost ms-auto" data-toast="Student insight opened|${s[1]}'s progress evidence is available in demo mode."><i class="bi bi-chevron-right"></i></button></div></div>`).join("")}
      </div></section>`;
    return appShell(content, `<button class="btn btn-ghost" data-toast="Faculty report prepared|The course analytics report is ready."><i class="bi bi-file-earmark-bar-graph me-2"></i>Reports</button><button class="btn btn-primary" data-toast="Announcement drafted|A class announcement draft has been opened."><i class="bi bi-megaphone me-2"></i>Announce</button>`);
  };

  const qr = () => Array.from({length:49},(_,i)=>`<span class="${[0,1,2,3,4,6,7,11,13,14,16,18,20,21,22,23,24,27,28,30,32,34,35,36,37,38,39,41,42,43,44,45,46,48].includes(i)?"dark":""}"></span>`).join("");

  const eventsPage = () => {
    const content = `
      <section class="event-featured mb-3"><div class="event-date"><span>Aug</span><strong>24</strong></div><span class="mini-badge ms-2" style="color:#fff;background:rgba(255,255,255,.13)">Featured event</span><h2>FutureTech 2026: AI, Industry & Innovation</h2><p>Meet researchers, founders, and student builders for a full day of talks, demos, and networking at the University Auditorium.</p><div class="d-flex flex-wrap gap-3 mt-4 small opacity-75"><span><i class="bi bi-clock me-1"></i>9:00 AM–5:00 PM</span><span><i class="bi bi-geo-alt me-1"></i>Main Auditorium</span><span><i class="bi bi-people me-1"></i>327 registered</span></div><button class="btn btn-light mt-4 event-register" data-event="FutureTech 2026">Register now <i class="bi bi-arrow-right ms-2"></i></button></section>
      <div class="row g-3 mb-3">
        ${[
          ["28","Aug","Research Poster Workshop","Learn how to turn a semester project into a clear academic poster.","Seminar Room 4","42 seats left"],
          ["03","Sep","Inter-Department Hackathon","Build an education-focused solution in a 24-hour team challenge.","Innovation Lab","18 teams joined"],
          ["10","Sep","Career Readiness Clinic","CV review, mock interviews, and professional profile feedback.","Career Center","26 seats left"]
        ].map(item=>`<div class="col-md-6 col-xl-4"><article class="card-ui hoverable event-card"><div class="d-flex gap-3"><div class="event-mini-date"><span>${item[1]}</span><strong>${item[0]}</strong></div><div><h3 class="h6 fw-bold mb-1">${item[2]}</h3><span class="text-muted small"><i class="bi bi-geo-alt me-1"></i>${item[4]}</span></div></div><p class="text-muted small mt-3">${item[3]}</p><div class="d-flex justify-content-between align-items-center mt-3"><span class="mini-badge neutral">${item[5]}</span><button class="btn btn-sm btn-soft-primary event-register" data-event="${item[2]}">Register</button></div></article></div>`).join("")}
      </div>
      <div class="row g-3"><div class="col-xl-7"><section class="card-ui h-100"><div class="card-head"><div><h2>Attendance dashboard</h2><p>Your event participation this academic year</p></div><span class="mini-badge success">78% attendance</span></div><div class="card-body-ui"><div class="row g-3 mb-4"><div class="col-4 text-center"><strong class="d-block fs-2">9</strong><span class="text-muted small">Registered</span></div><div class="col-4 text-center"><strong class="d-block fs-2">7</strong><span class="text-muted small">Attended</span></div><div class="col-4 text-center"><strong class="d-block fs-2">4</strong><span class="text-muted small">Certificates</span></div></div><div class="chart-wrap small"><canvas id="eventAttendanceChart"></canvas></div></div></section></div><div class="col-xl-5"><section class="card-ui h-100"><div class="card-head"><div><h2>My next ticket</h2><p>Scan at the event entrance</p></div><button class="btn btn-sm btn-ghost" id="openTicket">Expand</button></div><div class="card-body-ui"><div class="d-flex flex-wrap align-items-center gap-4"><div class="qr-preview">${qr()}</div><div><span class="course-code">Ticket #ASP-FT26-0142</span><h3 class="h6 fw-bold mt-2">FutureTech 2026</h3><p class="text-muted small mb-2">Najmu Alam Efti · Student</p><span class="mini-badge success"><i class="bi bi-check-circle"></i> Confirmed</span></div></div></div></section></div></div>
      <section class="card-ui mt-3"><div class="card-head"><div><h2>Certificate preview</h2><p>Certificates become available after verified attendance</p></div><button class="btn btn-sm btn-ghost" id="openCertificate">Full preview</button></div><div class="card-body-ui"><div class="certificate"><small>PREMIER UNIVERSITY</small><h3>Certificate of Participation</h3><p class="mb-1">This is proudly presented to</p><strong class="fs-5">Najmul Alam Efti</strong><p class="small mt-2 mb-0">for participating in <strong>Research & Innovation Week 2026</strong></p><div class="seal"><i class="bi bi-award"></i></div></div></div></section>`;
    return appShell(content, `<button class="btn btn-ghost" data-toast="Event calendar synced|Your registered events are ready to sync in the full platform."><i class="bi bi-calendar-plus me-2"></i>Sync calendar</button><button class="btn btn-primary" data-toast="Organizer workspace|Event creation is available to organizer accounts."><i class="bi bi-plus-lg me-2"></i>Create event</button>`);
  };

  const pages = {
    landing: landingPage,
    dashboard: dashboardPage,
    profile: profilePage,
    courses: coursesPage,
    assessments: assessmentsPage,
    planner: plannerPage,
    priorities: prioritiesPage,
    progress: progressPage,
    resources: resourcesPage,
    community: communityPage,
    faculty: facultyPage,
    events: eventsPage
  };

  function showToast(title, message, type = "success") {
    const stack = document.getElementById("toastStack");
    if (!stack) return;
    const toast = document.createElement("div");
    toast.className = "app-toast";
    toast.innerHTML = `<span class="toast-symbol"><i class="bi ${type === "success" ? "bi-check2" : "bi-info"}"></i></span><div><strong>${title}</strong><p>${message}</p></div><button class="btn-close ms-auto" aria-label="Close"></button>`;
    stack.appendChild(toast);
    const remove = () => { toast.classList.add("out"); setTimeout(() => toast.remove(), 260); };
    toast.querySelector(".btn-close").addEventListener("click", remove);
    setTimeout(remove, 3900);
  }

  function applyTheme(theme) {
    document.documentElement.dataset.theme = theme;
    document.querySelectorAll(".theme-toggle i").forEach(icon => {
      icon.className = `bi ${theme === "dark" ? "bi-sun" : "bi-moon-stars"}`;
    });
    document.querySelectorAll(".theme-toggle").forEach(button => button.setAttribute("aria-label", theme === "dark" ? "Switch to light mode" : "Switch to dark mode"));
  }

  function initializeTheme() {
    const saved = localStorage.getItem("studypilot-theme");
    const preferred = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    applyTheme(saved || preferred);
    document.querySelectorAll(".theme-toggle").forEach(button => button.addEventListener("click", () => {
      const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
      localStorage.setItem("studypilot-theme", next);
      applyTheme(next);
      initializeCharts(true);
      showToast(`${next === "dark" ? "Dark" : "Light"} mode enabled`, "Your display preference is saved on this device.");
    }));
  }

  function openModal(title, body, footer = "") {
    const modal = document.getElementById("appModal");
    if (!modal) return;
    document.getElementById("appModalTitle").textContent = title;
    document.getElementById("appModalBody").innerHTML = body;
    document.getElementById("appModalFooter").innerHTML = footer || `<button type="button" class="btn btn-ghost" data-bs-dismiss="modal" data-close-modal>Close</button>`;
    if (window.bootstrap && window.bootstrap.Modal) {
      window.bootstrap.Modal.getOrCreateInstance(modal).show();
    } else {
      modal.style.display = "block";
      modal.classList.add("show");
      modal.setAttribute("aria-modal", "true");
      modal.removeAttribute("aria-hidden");
      document.body.style.overflow = "hidden";
    }
  }

  function closeModalFallback() {
    const modal = document.getElementById("appModal");
    if (!modal || window.bootstrap) return;
    modal.style.display = "none";
    modal.classList.remove("show");
    modal.setAttribute("aria-hidden", "true");
    document.body.style.overflow = "";
  }

  function bindGlobalInteractions() {
    document.querySelectorAll("[data-toast]").forEach(button => button.addEventListener("click", event => {
      if (button.tagName === "A" && button.getAttribute("href")) return;
      event.preventDefault();
      const [title, message] = button.dataset.toast.split("|");
      showToast(title, message || "Action completed in demo mode.");
    }));
    document.querySelectorAll("[data-close-modal]").forEach(button => button.addEventListener("click", closeModalFallback));

    const sidebar = document.getElementById("appSidebar");
    const backdrop = document.getElementById("sidebarBackdrop");
    const closeSidebar = () => { sidebar?.classList.remove("open"); backdrop?.classList.remove("show"); };
    document.getElementById("mobileMenu")?.addEventListener("click", () => { sidebar?.classList.add("open"); backdrop?.classList.add("show"); });
    backdrop?.addEventListener("click", closeSidebar);
    window.addEventListener("resize", () => { if (window.innerWidth > 991) closeSidebar(); });

    document.getElementById("notificationButton")?.addEventListener("click", () => openModal("Notifications", `
      <div class="list-row"><span class="list-icon danger"><i class="bi bi-alarm"></i></span><div><div class="list-title">CT due in two days</div><div class="list-meta">Process Scheduling · Operating Systems</div></div><span class="mini-badge danger">New</span></div>
      <div class="list-row"><span class="list-icon teal"><i class="bi bi-chat-left-text"></i></span><div><div class="list-title">New reply in your study group</div><div class="list-meta">AI Final Sprint · 18 minutes ago</div></div></div>
      <div class="list-row"><span class="list-icon"><i class="bi bi-calendar-event"></i></span><div><div class="list-title">FutureTech registration confirmed</div><div class="list-meta">Your QR ticket is ready</div></div></div>`));

    const globalSearch = document.getElementById("globalSearch");
    globalSearch?.addEventListener("keydown", event => {
      if (event.key === "Enter" && globalSearch.value.trim()) {
        event.preventDefault();
        openModal("Search results", `<div class="empty-state show"><div class="empty-icon"><i class="bi bi-search"></i></div><h3 class="h5">search for “${globalSearch.value.replace(/[<>]/g, "")}”</h3><p class="text-muted">Global search is represented in the prototype. Course, resource, community, and event results will be connected in a later phase.</p></div>`);
      }
    });
  }

  function chartColors() {
    const styles = getComputedStyle(document.documentElement);
    return {
      primary: styles.getPropertyValue("--primary").trim() || "#2563eb",
      secondary: styles.getPropertyValue("--secondary").trim() || "#14b8a6",
      warning: styles.getPropertyValue("--warning").trim() || "#f59e0b",
      danger: styles.getPropertyValue("--danger").trim() || "#ef4444",
      success: styles.getPropertyValue("--success").trim() || "#22c55e",
      text: styles.getPropertyValue("--muted").trim() || "#64748b",
      line: styles.getPropertyValue("--line").trim() || "#e2e8f0"
    };
  }

  function makeChart(id, config) {
    const canvas = document.getElementById(id);
    if (!canvas) return;
    if (!window.Chart) {
      canvas.style.display = "none";
      const fallback = document.createElement("div");
      fallback.className = "chart-fallback";
      fallback.style.display = "flex";
      [38,52,45,68,57,82,72,90].forEach(value => fallback.insertAdjacentHTML("beforeend", `<span style="height:${value}%"></span>`));
      canvas.parentElement.appendChild(fallback);
      return;
    }
    charts.push(new window.Chart(canvas, config));
  }

  function initializeCharts(refresh = false) {
    if (refresh) {
      while (charts.length) charts.pop().destroy();
      document.querySelectorAll(".chart-fallback").forEach(el => el.remove());
      document.querySelectorAll("canvas").forEach(el => el.style.display = "block");
    }
    const c = chartColors();
    const grid = { color: c.line, drawBorder: false };
    const tick = { color: c.text, font: { size: 11 } };
    const baseOptions = { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false }, tooltip: { padding: 12, cornerRadius: 10, displayColors: false } }, scales: { x: { grid: { display: false }, ticks: tick, border: { display: false } }, y: { min: 0, grid, ticks: tick, border: { display: false } } } };

    makeChart("gpaTrendChart", { type: "line", data: { labels: ["Sem 1","Sem 2","Sem 3","Sem 4","Sem 5","Sem 6*"], datasets: [{ data: [3.08,3.18,3.24,3.31,3.42,3.52], borderColor: c.primary, backgroundColor: "rgba(37,99,235,.13)", fill: true, tension: .42, borderWidth: 3, pointRadius: 4, pointBackgroundColor: c.primary, pointBorderWidth: 3, pointBorderColor: getComputedStyle(document.documentElement).getPropertyValue("--surface") }] }, options: { ...baseOptions, scales: { ...baseOptions.scales, y: { ...baseOptions.scales.y, min: 2.5, max: 4, ticks: { ...tick, stepSize: .5 } } } } });
    makeChart("studyHoursChart", { type: "bar", data: { labels: ["Sun","Mon","Tue","Wed","Thu","Fri","Sat"], datasets: [{ data: [3.2,2.1,2.8,1.5,2.4,1.2,1.3], backgroundColor: [c.primary,c.primary,c.primary,c.secondary,c.primary,c.secondary,c.primary], borderRadius: 7, barThickness: 18 }] }, options: { ...baseOptions, scales: { ...baseOptions.scales, y: { ...baseOptions.scales.y, max: 4, ticks: { ...tick, stepSize: 1 } } } } });
    makeChart("progressHoursChart", { type: "bar", data: { labels: ["W1","W2","W3","W4","W5","W6","W7","W8"], datasets: [{ data: [9.5,11,10.2,12.8,11.6,13.1,12.9,14.5], backgroundColor: c.primary, borderRadius: 8, barThickness: 24 }, { data: [18,18,18,18,18,18,18,18], type: "line", borderColor: c.secondary, pointRadius: 0, borderDash: [6,6], borderWidth: 2 }] }, options: baseOptions });
    makeChart("facultyPerformanceChart", { type: "bar", data: { labels: ["ER model","Relational algebra","SQL","Normalization","Transactions","Indexing"], datasets: [{ data: [82,71,78,64,73,69], backgroundColor: [c.primary,c.primary,c.secondary,c.warning,c.primary,c.primary], borderRadius: 8, barThickness: 24 }] }, options: { ...baseOptions, scales: { ...baseOptions.scales, y: { ...baseOptions.scales.y, max: 100 } } } });
    makeChart("facultyWeakChart", { type: "doughnut", data: { labels: ["Normalization","Relational algebra","Transactions","Other"], datasets: [{ data: [36,24,18,22], backgroundColor: [c.danger,c.warning,c.primary,c.secondary], borderWidth: 0, spacing: 3 }] }, options: { responsive:true, maintainAspectRatio:false, cutout:"68%", plugins:{ legend:{ position:"bottom", labels:{ color:c.text, usePointStyle:true, padding:16, font:{size:10} } } } } });
    makeChart("eventAttendanceChart", { type: "line", data: { labels: ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug"], datasets: [{ data: [1,1,2,2,4,5,6,7], borderColor:c.secondary, backgroundColor:"rgba(20,184,166,.12)", fill:true, tension:.35, borderWidth:3, pointRadius:3 }] }, options: { ...baseOptions, scales:{...baseOptions.scales,y:{...baseOptions.scales.y,max:8,ticks:{...tick,stepSize:2}}} } });
  }

  function initializeProfile() {
    const edit = () => openModal("Edit profile", `<div class="row g-3"><div class="col-md-6"><label class="form-label">Full name</label><input class="form-control" value="NAjmul Alam Efti"></div><div class="col-md-6"><label class="form-label">Phone</label><input class="form-control" value="+880 18•• ••• 5256"></div><div class="col-12"><label class="form-label">Study bio</label><textarea class="form-control" rows="3">Focused on improving core CSE performance and building a sustainable study routine.</textarea></div></div>`, `<button class="btn btn-ghost" data-bs-dismiss="modal" data-close-modal>Cancel</button><button class="btn btn-primary" id="saveProfileModal">Save changes</button>`);
    document.getElementById("editProfileButton")?.addEventListener("click", edit);
    document.getElementById("editProfileTop")?.addEventListener("click", edit);
    document.addEventListener("click", event => { if (event.target.closest("#saveProfileModal")) { closeModalFallback(); showToast("Profile updated", "Your demo profile changes have been saved."); if (window.bootstrap) window.bootstrap.Modal.getInstance(document.getElementById("appModal"))?.hide(); } });
  }

  function initializeCourses() {
    document.querySelectorAll("#courseFilters .filter-btn").forEach(button => button.addEventListener("click", () => {
      document.querySelectorAll("#courseFilters .filter-btn").forEach(btn => btn.classList.remove("active"));
      button.classList.add("active");
      let visible = 0;
      document.querySelectorAll(".course-item").forEach(item => { const show = button.dataset.filter === "all" || item.dataset.state === button.dataset.filter; item.style.display = show ? "block" : "none"; if (show) visible++; });
      document.getElementById("courseEmpty")?.classList.toggle("show", visible === 0);
    }));
  }

  function initializeAssessments() {
    let type = "all";
    let status = "all";
    const apply = () => {
      let visible = 0;
      document.querySelectorAll("#assessmentTable tr").forEach(row => { const show = (type === "all" || row.dataset.type === type) && (status === "all" || row.dataset.status === status); row.style.display = show ? "table-row" : "none"; if (show) visible++; });
      document.getElementById("assessmentEmpty")?.classList.toggle("show", visible === 0);
    };
    document.querySelectorAll("#assessmentFilters .filter-btn").forEach(button => button.addEventListener("click", () => { document.querySelectorAll("#assessmentFilters .filter-btn").forEach(btn=>btn.classList.remove("active")); button.classList.add("active"); type=button.dataset.filter; apply(); }));
    document.getElementById("assessmentStatus")?.addEventListener("change", event => { status=event.target.value; apply(); });
  }

  function initializePlanner() {
    const setTab = tab => {
      document.querySelectorAll("[data-planner-tab]").forEach(button => button.classList.toggle("active", button.dataset.plannerTab === tab));
      document.querySelectorAll("[data-planner-panel]").forEach(panel => panel.classList.toggle("active", panel.dataset.plannerPanel === tab));
    };
    document.querySelectorAll("[data-planner-tab]").forEach(button => button.addEventListener("click", () => { setTab(button.dataset.plannerTab); history.replaceState(null,"",`#${button.dataset.plannerTab === "cgpa" ? "target" : button.dataset.plannerTab}`); }));
    const hash = location.hash.replace("#", "");
    if (hash === "study") setTab("study");
    if (hash === "target" || hash === "cgpa") setTab("cgpa");
    if (hash === "gpa") setTab("gpa");

    const calculateGpa = () => {
      let points = 0, credits = 0;
      document.querySelectorAll(".gpa-course-row").forEach(row => { const c = Number(row.querySelector(".gpa-credit").value) || 0; const g = Number(row.querySelector(".gpa-grade").value) || 0; credits += c; points += c*g; });
      const gpa = credits ? points/credits : 0;
      document.getElementById("gpaResult").textContent = gpa.toFixed(2);
      document.getElementById("gpaCredits").textContent = credits.toFixed(1);
      document.getElementById("gpaMeter").style.width = `${Math.min(100,gpa/4*100)}%`;
      document.getElementById("gpaMessage").textContent = gpa >= 3.75 ? "Excellent projection" : gpa >= 3 ? "Good standing" : "Needs recovery";
      showToast("GPA recalculated", `Projected semester GPA: ${gpa.toFixed(2)} across ${credits.toFixed(1)} credits.`);
    };
    document.getElementById("calculateGpa")?.addEventListener("click", calculateGpa);
    document.getElementById("addGpaCourse")?.addEventListener("click", () => {
      const rows = document.getElementById("gpaRows");
      const number = rows.children.length + 1;
      rows.insertAdjacentHTML("beforeend", `<div class="course-input-row gpa-course-row"><div class="course-name"><label class="form-label">Course ${number}</label><input class="form-control" placeholder="Course name"></div><div><label class="form-label">Credits</label><input class="form-control gpa-credit" type="number" min=".5" max="6" step=".5" value="3"></div><div><label class="form-label">Expected grade</label><select class="form-select gpa-grade">${gradeOptions(3.5)}</select></div><button class="btn btn-icon btn-ghost remove-gpa-row" type="button" aria-label="Remove course"><i class="bi bi-trash3"></i></button></div>`);
    });
    document.getElementById("gpaRows")?.addEventListener("click", event => { const button = event.target.closest(".remove-gpa-row"); if (button && document.querySelectorAll(".gpa-course-row").length > 1) button.closest(".gpa-course-row").remove(); });

    document.getElementById("calculateTarget")?.addEventListener("click", () => {
      const current=Number(document.getElementById("currentCgpa").value), completed=Number(document.getElementById("completedCredits").value), target=Number(document.getElementById("targetCgpa").value), remaining=Number(document.getElementById("remainingCredits").value);
      if ([current,completed,target,remaining].some(value=>!Number.isFinite(value)) || remaining<=0 || current<0 || current>4 || target<0 || target>4) { showToast("Check the scenario", "Enter valid CGPA values from 0 to 4 and at least one remaining credit.", "info"); return; }
      const required=(target*(completed+remaining)-current*completed)/remaining;
      const display=Math.max(0,required);
      document.getElementById("requiredGpa").textContent=display.toFixed(2);
      document.getElementById("targetNarrative").textContent=`Across your remaining ${remaining} credits to reach a final CGPA of ${target.toFixed(2)}.`;
      document.getElementById("targetMeter").style.width=`${Math.min(100,Math.max(0,display/4*100))}%`;
      document.getElementById("targetLevel").textContent=required>4?"Not mathematically reachable":required>=3.75?"Ambitious":required>=3.25?"Achievable":"Comfortable";
      document.getElementById("targetTip").innerHTML=required>4?`<i class="bi bi-info-circle me-2"></i>At the current inputs, the highest possible final CGPA is ${((current*completed+4*remaining)/(completed+remaining)).toFixed(2)}.`:`<i class="bi bi-stars me-2"></i>Prioritize high-credit courses and protect your strongest assessment types.`;
      showToast("Target scenario updated", required>4?"This target needs adjusted credits or a revised final CGPA.":`You need an average GPA of ${required.toFixed(2)}.`);
    });

    document.querySelectorAll(".day-pill").forEach(day => day.addEventListener("click", () => { document.querySelectorAll(".day-pill").forEach(d=>d.classList.remove("active")); day.classList.add("active"); showToast(`${day.dataset.day} plan selected`, "The prototype keeps Sunday blocks as representative schedule data."); }));
    document.getElementById("optimizePlan")?.addEventListener("click", () => showToast("Plan optimized", "Two high-energy blocks were moved earlier and break spacing was improved."));
    document.getElementById("savePlan")?.addEventListener("click", () => showToast("Scenario saved", "Your GPA assumptions and study plan are saved locally for this demo session."));
  }

  function initializePriorities() {
    document.querySelectorAll("#priorityFilters .filter-btn").forEach(button => button.addEventListener("click",()=>{document.querySelectorAll("#priorityFilters .filter-btn").forEach(btn=>btn.classList.remove("active"));button.classList.add("active");document.querySelectorAll(".priority-item").forEach(item=>item.style.display=button.dataset.filter==="all"||item.dataset.level===button.dataset.filter?"block":"none");}));
  }

  function initializeResources() {
    let type="all";
    const search=document.getElementById("resourceSearch"), course=document.getElementById("resourceCourse");
    const apply=()=>{const query=search.value.trim().toLowerCase(), selected=course.value;let count=0;document.querySelectorAll(".resource-item").forEach(item=>{const show=(type==="all"||item.dataset.type===type)&&(!query||item.dataset.search.includes(query))&&(selected==="all"||item.dataset.course===selected);item.style.display=show?"block":"none";if(show)count++;});document.getElementById("resourceCount").textContent=count;document.getElementById("resourceEmpty").classList.toggle("show",count===0);};
    document.querySelectorAll("#resourceFilters .filter-btn").forEach(button=>button.addEventListener("click",()=>{document.querySelectorAll("#resourceFilters .filter-btn").forEach(btn=>btn.classList.remove("active"));button.classList.add("active");type=button.dataset.filter;apply();}));
    search?.addEventListener("input",apply);course?.addEventListener("change",apply);
    document.getElementById("clearResourceFilters")?.addEventListener("click",()=>{search.value="";course.value="all";type="all";document.querySelectorAll("#resourceFilters .filter-btn").forEach((btn,index)=>btn.classList.toggle("active",index===0));apply();});
    document.querySelectorAll(".save-resource").forEach(button=>button.addEventListener("click",()=>{const icon=button.querySelector("i"),saved=icon.classList.contains("bi-bookmark-fill");icon.className=`bi ${saved?"bi-bookmark":"bi-bookmark-fill"}`;button.classList.toggle("text-primary",!saved);showToast(saved?"Removed from saved":"Resource saved",saved?"This item was removed from your saved resources.":"You can find this item in Saved Resources.");}));
    document.getElementById("suggestResource")?.addEventListener("click",()=>openModal("Suggest a learning resource",`<div class="row g-3"><div class="col-12"><label class="form-label">Resource title</label><input class="form-control" placeholder="Enter a clear title"></div><div class="col-md-6"><label class="form-label">Resource type</label><select class="form-select"><option>PDF</option><option>Video</option><option>Notes</option></select></div><div class="col-md-6"><label class="form-label">Course</label><select class="form-select">${courseData.map(c=>`<option>${c.title}</option>`).join("")}</select></div><div class="col-12"><label class="form-label">Public URL</label><input class="form-control" placeholder="https://"></div></div>`,`<button class="btn btn-ghost" data-bs-dismiss="modal" data-close-modal>Cancel</button><button class="btn btn-primary" id="submitResource">Submit suggestion</button>`));
    document.addEventListener("click",event=>{if(event.target.closest("#submitResource")){if(window.bootstrap)window.bootstrap.Modal.getInstance(document.getElementById("appModal"))?.hide();else closeModalFallback();showToast("Suggestion submitted","A moderator will review the resource before publication.");}});
  }

  function initializeCommunity() {
    document.getElementById("publishPost")?.addEventListener("click",()=>{const input=document.getElementById("communityComposer");if(!input.value.trim()){showToast("Write something first","Add a question or academic update before publishing.","info");return;}const safe=input.value.replace(/[<>]/g,"");document.getElementById("communityFeed").insertAdjacentHTML("afterbegin",`<article class="card-ui post-card mb-3"><div class="post-header"><span class="avatar">MR</span><div><strong>Najmul Alam Efti</strong><small>Study update · Just now</small></div></div><h3 class="post-title">New community update</h3><p class="post-copy">${safe}</p><div class="post-actions"><button class="post-action like-button"><i class="bi bi-hand-thumbs-up me-1"></i><span>0</span></button><button class="post-action"><i class="bi bi-chat-left-text me-1"></i>0 comments</button></div></article>`);input.value="";showToast("Post published","Your update is now visible in the demo community feed.");});
    document.getElementById("communityFeed")?.addEventListener("click",event=>{const button=event.target.closest(".like-button");if(!button)return;const count=button.querySelector("span"),liked=button.classList.toggle("liked");button.querySelector("i").className=`bi ${liked?"bi-hand-thumbs-up-fill":"bi-hand-thumbs-up"} me-1`;count.textContent=Number(count.textContent)+(liked?1:-1);});
    document.querySelectorAll(".group-button").forEach(button=>button.addEventListener("click",()=>showToast("Study group opened","Group chat and sessions are represented as a demo interaction.")));
  }

  function ticketBody(eventName="FutureTech 2026") { return `<div class="text-center"><div class="qr-preview mx-auto mb-4">${qr()}</div><span class="course-code">Ticket #ASP-FT26-0142</span><h3 class="h5 fw-bold mt-2">${eventName}</h3><p class="text-muted">Najmul Alam Efti · Student · Confirmed</p><span class="mini-badge success"><i class="bi bi-check-circle"></i> Ready to scan</span></div>`; }

  function initializeEvents() {
    document.querySelectorAll(".event-register").forEach(button=>button.addEventListener("click",()=>{const name=button.dataset.event;button.textContent="Registered";button.disabled=true;showToast("Registration confirmed",`Your QR ticket for ${name} is ready.`);setTimeout(()=>openModal("Registration ticket",ticketBody(name)),350);}));
    document.getElementById("openTicket")?.addEventListener("click",()=>openModal("Your QR ticket",ticketBody()));
    document.getElementById("openCertificate")?.addEventListener("click",()=>openModal("Certificate preview",`<div class="certificate"><small>PREMIER UNIVERSITY</small><h3>Certificate of Participation</h3><p class="mb-1">This is proudly presented to</p><strong class="fs-4">Najmul Alam Efti</strong><p class="small mt-2 mb-0">for participating in <strong>Research & Innovation Week 2026</strong></p><div class="seal"><i class="bi bi-award"></i></div></div>`));
  }

  function initializePage() {
    const initializers={profile:initializeProfile,courses:initializeCourses,assessments:initializeAssessments,planner:initializePlanner,priorities:initializePriorities,resources:initializeResources,community:initializeCommunity,events:initializeEvents};
    initializers[currentPage]?.();
    if (["dashboard","progress","faculty","events"].includes(currentPage)) initializeCharts();
  }

  function render() {
    const renderer=pages[currentPage]||pages.dashboard;
    app.innerHTML=renderer();
    initializeTheme();
    bindGlobalInteractions();
    initializePage();
    requestAnimationFrame(()=>setTimeout(()=>document.getElementById("pageLoader")?.classList.add("is-hidden"),420));
  }

  render();
})();
