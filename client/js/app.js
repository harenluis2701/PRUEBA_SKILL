// SkillHub Client Application - Vanilla JS & Bootstrap
const API_BASE_URL = 'http://localhost:5050/api/v1';

// Detailed examples of what each workshop teaches
const WORKSHOP_SYLLABUS = {
    'FOT-001': [
        'Regla de tercios y composición en smartphone',
        'Manejo de luz natural y contra-luz',
        'Retoque profesional con Lightroom Mobile'
    ],
    'COC-001': [
        'Amasado al dente: harina 00 y sémola',
        'Raviolis, fettuccine y tagliatelle desde cero',
        'Salsas tradicionales: Pomodoro, Pesto y Cacio e Pepe'
    ],
    'ORA-001': [
        'Control del pánico escénico y respiración diafragmática',
        'Estructura de discursos persuasivos (Storytelling)',
        'Lenguaje corporal, modulación y contacto visual'
    ],
    'DIS-001': [
        'Jerarquía tipográfica y combinaciones de fuentes',
        'Psicología del color y contraste accesible',
        'Creación de layouts limpios para marcas digitales'
    ]
};

// State Management via localStorage
const AuthState = {
    getToken() {
        return localStorage.getItem('skillhub_token');
    },
    getUser() {
        const user = localStorage.getItem('skillhub_user');
        return user ? JSON.parse(user) : null;
    },
    setAuth(token, user) {
        localStorage.setItem('skillhub_token', token);
        localStorage.setItem('skillhub_user', JSON.stringify(user));
        renderNavbar();
    },
    clear() {
        localStorage.removeItem('skillhub_token');
        localStorage.removeItem('skillhub_user');
        renderNavbar();
    },
    isAuthenticated() {
        return !!this.getToken();
    },
    isParticipant() {
        const u = this.getUser();
        return u && u.role === 'Participant';
    },
    isEmployee() {
        const u = this.getUser();
        return u && u.role === 'Employee';
    }
};

// UI Navigation Controller
function showSection(sectionName) {
    const sections = ['workshops', 'my-enrollments', 'employee-panel', 'login', 'register'];
    sections.forEach(s => {
        const el = document.getElementById(`section-${s}`);
        const nav = document.getElementById(`nav-${s}`);
        if (el) el.classList.add('d-none');
        if (nav) nav.classList.remove('active');
    });

    const targetEl = document.getElementById(`section-${sectionName}`);
    const targetNav = document.getElementById(`nav-${sectionName}`);

    if (targetEl) targetEl.classList.remove('d-none');
    if (targetNav) targetNav.classList.add('active');

    if (sectionName === 'workshops') loadWorkshops();
    if (sectionName === 'my-enrollments') {
        if (!AuthState.isAuthenticated()) {
            showAlert('Por favor inicia sesión para ver tus inscripciones.', 'warning');
            showSection('login');
            return;
        }
        loadMyEnrollments();
    }
    if (sectionName === 'employee-panel') {
        if (!AuthState.isEmployee()) {
            showAlert('Acceso denegado: Esta sección es exclusiva para empleados.', 'danger');
            showSection('workshops');
            return;
        }
        loadEmployeePanel();
    }
}

// Global Alert Banner
function showAlert(message, type = 'danger') {
    const container = document.getElementById('alert-container');
    container.innerHTML = `
        <div class="alert alert-${type} alert-dismissible fade show shadow-sm" role="alert">
            <i class="bi ${type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill'} me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function clearAlert() {
    const container = document.getElementById('alert-container');
    container.innerHTML = '';
}

// Navbar Authentication State
function renderNavbar() {
    const navButtons = document.getElementById('nav-auth-buttons');
    const myEnrollmentsLi = document.getElementById('nav-my-enrollments-li');
    const employeePanelLi = document.getElementById('nav-employee-panel-li');

    if (AuthState.isAuthenticated()) {
        const user = AuthState.getUser();

        if (AuthState.isParticipant()) {
            myEnrollmentsLi.classList.remove('d-none');
            employeePanelLi.classList.add('d-none');
        } else if (AuthState.isEmployee()) {
            myEnrollmentsLi.classList.add('d-none');
            employeePanelLi.classList.remove('d-none');
        }

        navButtons.innerHTML = `
            <span class="navbar-text text-light small me-2">
                <i class="bi bi-person-circle me-1 text-warning"></i> <strong>${user.fullName}</strong> 
                <span class="badge ${AuthState.isEmployee() ? 'bg-warning text-dark' : 'bg-light text-dark'} ms-1">${user.role}</span>
            </span>
            <button class="btn btn-outline-light btn-sm px-3" onclick="handleLogout()">
                <i class="bi bi-box-arrow-right me-1"></i> Salir
            </button>
        `;
    } else {
        myEnrollmentsLi.classList.add('d-none');
        employeePanelLi.classList.add('d-none');
        navButtons.innerHTML = `
            <button class="btn btn-outline-light btn-sm px-3" onclick="showSection('login')">
                <i class="bi bi-box-arrow-in-right me-1"></i> Iniciar Sesión
            </button>
            <button class="btn btn-primary btn-sm px-3 fw-semibold" onclick="showSection('register')">
                <i class="bi bi-person-plus me-1"></i> Registrarse
            </button>
        `;
    }
}

// 1. Load Workshops Catalog
async function loadWorkshops() {
    clearAlert();
    const container = document.getElementById('workshops-container');
    container.innerHTML = `
        <div class="col-12 text-center py-5">
            <div class="spinner-border text-success" role="status"></div>
            <p class="text-muted mt-2">Cargando talleres disponibles...</p>
        </div>
    `;

    try {
        const response = await fetch(`${API_BASE_URL}/workshops`);
        if (!response.ok) throw new Error('No se pudo obtener el catálogo de talleres.');
        const workshops = await response.json();

        // Check active enrollments if user is participant
        const enrolledWorkshopIds = new Set();
        if (AuthState.isAuthenticated() && AuthState.isParticipant()) {
            try {
                const myRes = await fetch(`${API_BASE_URL}/enrollments/mine`, {
                    headers: { 'Authorization': `Bearer ${AuthState.getToken()}` }
                });
                if (myRes.ok) {
                    const myEnrollments = await myRes.json();
                    myEnrollments
                        .filter(e => e.status === 'Active')
                        .forEach(e => enrolledWorkshopIds.add(e.workshopId));
                }
            } catch (ignore) {}
        }

        if (workshops.length === 0) {
            container.innerHTML = `
                <div class="col-12 text-center py-5">
                    <i class="bi bi-calendar-x fs-1 text-muted"></i>
                    <p class="text-muted mt-2">No hay talleres futuros disponibles en este momento.</p>
                </div>
            `;
            return;
        }

        container.innerHTML = workshops.map(w => {
            const startDate = new Date(w.startDateTime).toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' });
            const endDate = new Date(w.endDateTime).toLocaleString('es-CO', { timeStyle: 'short' });
            const spotsClass = w.availableSpots > 0 ? 'bg-success-subtle text-success border border-success-subtle' : 'bg-danger-subtle text-danger border border-danger-subtle';
            const hasSpots = w.availableSpots > 0;
            const isAlreadyEnrolled = enrolledWorkshopIds.has(w.id);

            const syllabusItems = WORKSHOP_SYLLABUS[w.code] || [
                'Técnicas fundamentales y ejercicios guiados',
                'Metodología práctica con retroalimentación en vivo'
            ];

            return `
                <div class="col-md-6 col-lg-4">
                    <div class="card card-workshop h-100 p-3 ${isAlreadyEnrolled ? 'enrolled-card' : ''}">
                        <div class="card-body d-flex flex-column p-0">
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <span class="badge badge-code">${w.code}</span>
                                <div class="d-flex align-items-center gap-1">
                                    ${isAlreadyEnrolled ? `
                                        <span class="badge bg-success text-white badge-spots">
                                            <i class="bi bi-check-circle-fill me-1"></i> Inscrito
                                        </span>
                                    ` : ''}
                                    <span class="badge ${spotsClass} badge-spots">
                                        <i class="bi bi-people-fill me-1"></i> ${w.availableSpots}/${w.maxCapacity}
                                    </span>
                                </div>
                            </div>

                            <h6 class="fw-bold text-dark mb-1 fs-6">${w.title}</h6>
                            <p class="card-text text-muted small mb-2 flex-grow-0" style="font-size: 0.82rem;">${w.description}</p>

                            <!-- Mini Syllabus -->
                            <div class="workshop-topics mb-3 flex-grow-1">
                                <div class="small fw-bold text-dark mb-1" style="font-size: 0.75rem;">
                                    <i class="bi bi-lightbulb-fill text-warning me-1"></i> Qué aprenderás:
                                </div>
                                <ul>
                                    ${syllabusItems.map(item => `<li>${item}</li>`).join('')}
                                </ul>
                            </div>

                            <div class="border-top pt-2 mt-auto">
                                <div class="small text-secondary mb-1" style="font-size: 0.8rem;">
                                    <i class="bi bi-person-fill text-success me-1"></i> <strong>Instructor:</strong> ${w.instructor.fullName}
                                </div>
                                <div class="small text-secondary mb-3" style="font-size: 0.8rem;">
                                    <i class="bi bi-clock-fill text-success me-1"></i> <strong>Horario:</strong> ${startDate}
                                </div>

                                ${isAlreadyEnrolled ? `
                                    <button class="btn btn-enrolled w-100 py-2 small" disabled>
                                        <i class="bi bi-check-lg me-1"></i> Inscrito en este taller
                                    </button>
                                ` : `
                                    <button class="btn btn-primary w-100 py-2 small fw-semibold ${!hasSpots ? 'disabled' : ''}" 
                                            onclick="handleEnroll('${w.id}')">
                                        <i class="bi bi-plus-circle me-1"></i> ${hasSpots ? 'Inscribirme al Taller' : 'Sin Cupos'}
                                    </button>
                                `}
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }).join('');
    } catch (err) {
        container.innerHTML = `
            <div class="col-12 text-center py-5 text-danger">
                <i class="bi bi-exclamation-octagon fs-1"></i>
                <p class="mt-2">${err.message}</p>
            </div>
        `;
    }
}

// 2. Handle Enrollment (Participant)
async function handleEnroll(workshopId) {
    if (!AuthState.isAuthenticated()) {
        showAlert('Debes iniciar sesión para inscribirte en un taller.', 'warning');
        showSection('login');
        return;
    }

    if (!AuthState.isParticipant()) {
        showAlert('Estás conectado como empleado. Para inscribirte en talleres usa una cuenta de participante.', 'warning');
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/enrollments`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${AuthState.getToken()}`
            },
            body: JSON.stringify({ workshopId })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'No se pudo completar la inscripción.');
        }

        showAlert(`¡Inscripción exitosa en el taller "${data.workshopTitle}"!`, 'success');
        loadWorkshops();
    } catch (err) {
        showAlert(err.message, 'danger');
    }
}

// 3. Load My Enrollments (Participant)
async function loadMyEnrollments() {
    clearAlert();
    const tbody = document.getElementById('my-enrollments-body');
    tbody.innerHTML = `
        <tr>
            <td colspan="6" class="text-center py-4">
                <div class="spinner-border spinner-border-sm text-success" role="status"></div>
                <span class="ms-2 text-muted">Cargando tus inscripciones...</span>
            </td>
        </tr>
    `;

    try {
        const response = await fetch(`${API_BASE_URL}/enrollments/mine`, {
            headers: {
                'Authorization': `Bearer ${AuthState.getToken()}`
            }
        });

        if (!response.ok) throw new Error('No se pudieron obtener tus inscripciones.');

        const enrollments = await response.json();

        if (enrollments.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center py-4 text-muted">
                        No tienes inscripciones registradas. ¡Visita el catálogo para unirte a un taller!
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = enrollments.map(e => {
            const isCancelled = e.status === 'Cancelled';
            const statusBadge = isCancelled 
                ? '<span class="badge bg-secondary">Cancelada</span>'
                : '<span class="badge bg-success">Activa</span>';

            const startStr = new Date(e.workshopStart).toLocaleString('es-CO', { dateStyle: 'short', timeStyle: 'short' });
            const enrolledStr = new Date(e.enrolledAt).toLocaleString('es-CO', { dateStyle: 'short', timeStyle: 'short' });

            return `
                <tr>
                    <td>
                        <span class="badge badge-code me-1">${e.workshopCode}</span>
                        <strong>${e.workshopTitle}</strong>
                    </td>
                    <td>${e.instructorName}</td>
                    <td><small class="text-secondary">${startStr}</small></td>
                    <td><small class="text-secondary">${enrolledStr}</small></td>
                    <td>${statusBadge}</td>
                    <td class="text-end">
                        <div class="btn-group btn-group-sm">
                            ${!isCancelled ? `
                                <button class="btn btn-outline-danger" title="Cancelar inscripción" onclick="handleCancel('${e.id}', '${e.workshopTitle}')">
                                    <i class="bi bi-x-circle me-1"></i> Cancelar
                                </button>
                                <button class="btn btn-outline-success" title="Descargar Constancia PDF" onclick="handleDownloadCertificate('${e.id}')">
                                    <i class="bi bi-file-earmark-pdf me-1"></i> Descargar PDF
                                </button>
                            ` : `
                                <span class="text-muted small fst-italic">Cancelada</span>
                            `}
                        </div>
                    </td>
                </tr>
            `;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" class="text-center py-4 text-danger">${err.message}</td>
            </tr>
        `;
    }
}

// 4. Cancel Enrollment
async function handleCancel(enrollmentId, workshopTitle) {
    if (!confirm(`¿Estás seguro de cancelar tu inscripción al taller "${workshopTitle}"?\n\nRecuerda: solo puedes cancelar con al menos 24 horas de anticipación.`)) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/enrollments/${enrollmentId}/cancel`, {
            method: 'PATCH',
            headers: {
                'Authorization': `Bearer ${AuthState.getToken()}`
            }
        });

        if (!response.ok) {
            const data = await response.json();
            throw new Error(data.error || 'No se pudo cancelar la inscripción.');
        }

        showAlert(`La inscripción al taller "${workshopTitle}" ha sido cancelada y el cupo fue liberado.`, 'success');
        if (AuthState.isParticipant()) {
            loadMyEnrollments();
        } else {
            loadWorkshopAttendees();
        }
    } catch (err) {
        showAlert(err.message, 'danger');
    }
}

// 5. Download Certificate PDF
async function handleDownloadCertificate(enrollmentId) {
    try {
        const response = await fetch(`${API_BASE_URL}/enrollments/${enrollmentId}/certificate`, {
            headers: {
                'Authorization': `Bearer ${AuthState.getToken()}`
            }
        });

        if (!response.ok) {
            const data = await response.json();
            throw new Error(data.error || 'No se pudo descargar la constancia de inscripción.');
        }

        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Constancia_Inscripcion_${enrollmentId}.pdf`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
    } catch (err) {
        showAlert(err.message, 'danger');
    }
}

// ==========================================
// 6. EMPLOYEE PANEL LOGIC (NEW!)
// ==========================================

let allWorkshopsCache = [];

function switchEmployeeTab(tabName) {
    ['attendees', 'top', 'import'].forEach(t => {
        document.getElementById(`tab-content-${t}`).classList.add('d-none');
        document.getElementById(`tab-${t}-btn`).classList.remove('active');
    });

    document.getElementById(`tab-content-${tabName}`).classList.remove('d-none');
    document.getElementById(`tab-${tabName}-btn`).classList.add('active');

    if (tabName === 'top') loadTopWorkshopsReport();
}

async function loadEmployeePanel() {
    clearAlert();
    try {
        // Load workshops for dropdown
        const res = await fetch(`${API_BASE_URL}/workshops`);
        if (res.ok) {
            allWorkshopsCache = await res.json();
            const select = document.getElementById('employee-workshop-select');
            select.innerHTML = '<option value="">-- Selecciona un taller --</option>' +
                allWorkshopsCache.map(w => `<option value="${w.id}">${w.code} — ${w.title}</option>`).join('');
        }
    } catch (err) {
        showAlert(err.message, 'danger');
    }
}

async function loadWorkshopAttendees() {
    const workshopId = document.getElementById('employee-workshop-select').value;
    const tbody = document.getElementById('workshop-attendees-body');
    const statsContainer = document.getElementById('workshop-capacity-stats');

    if (!workshopId) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-muted">Selecciona un taller arriba para ver los inscritos.</td></tr>';
        statsContainer.innerHTML = '';
        return;
    }

    const workshop = allWorkshopsCache.find(w => w.id === workshopId);

    tbody.innerHTML = `
        <tr>
            <td colspan="5" class="text-center py-4">
                <div class="spinner-border spinner-border-sm text-success" role="status"></div>
                <span class="ms-2 text-muted">Consultando inscritos...</span>
            </td>
        </tr>
    `;

    try {
        const res = await fetch(`${API_BASE_URL}/workshops/${workshopId}/enrollments`, {
            headers: { 'Authorization': `Bearer ${AuthState.getToken()}` }
        });

        if (!res.ok) throw new Error('No se pudieron consultar los inscritos de este taller.');

        const attendees = await res.json();
        const activeCount = attendees.filter(a => a.status === 'Active').length;
        const maxCap = workshop ? workshop.maxCapacity : attendees.length;

        statsContainer.innerHTML = `
            <div class="d-flex align-items-center justify-content-end gap-3">
                <div class="text-end">
                    <span class="badge bg-success-subtle text-success fs-6 px-3 py-2 border border-success-subtle">
                        <i class="bi bi-people-fill me-1"></i> ${activeCount} inscritos activos / ${maxCap} cupos máx.
                    </span>
                </div>
            </div>
        `;

        if (attendees.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="text-center py-4 text-muted">
                        No hay personas inscritas aún en este taller.
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = attendees.map(a => {
            const isCancelled = a.status === 'Cancelled';
            const statusBadge = isCancelled 
                ? '<span class="badge bg-secondary">Cancelada</span>'
                : '<span class="badge bg-success">Activa</span>';

            const enrolledDate = new Date(a.enrolledAt).toLocaleString('es-CO', { dateStyle: 'short', timeStyle: 'short' });

            return `
                <tr>
                    <td><strong>${a.participantName}</strong></td>
                    <td><code>${a.participantEmail}</code></td>
                    <td><small class="text-secondary">${enrolledDate}</small></td>
                    <td>${statusBadge}</td>
                    <td class="text-end">
                        <div class="btn-group btn-group-sm">
                            <button class="btn btn-outline-success" title="Descargar Constancia PDF" onclick="handleDownloadCertificate('${a.id}')" ${isCancelled ? 'disabled' : ''}>
                                <i class="bi bi-file-earmark-pdf me-1"></i> PDF
                            </button>
                            ${!isCancelled ? `
                                <button class="btn btn-outline-danger" title="Cancelar inscripción como Empleado" onclick="handleCancel('${a.id}', '${a.workshopTitle}')">
                                    <i class="bi bi-x-circle me-1"></i> Cancelar
                                </button>
                            ` : `
                                <span class="badge bg-light text-muted ms-1">Cancelada por: ${a.cancelledBy || 'Staff'}</span>
                            `}
                        </div>
                    </td>
                </tr>
            `;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center py-4 text-danger">${err.message}</td></tr>`;
    }
}

// Top Workshops Report
async function loadTopWorkshopsReport() {
    const tbody = document.getElementById('top-workshops-body');
    tbody.innerHTML = `
        <tr>
            <td colspan="7" class="text-center py-4">
                <div class="spinner-border spinner-border-sm text-success" role="status"></div>
                <span class="ms-2 text-muted">Generando reporte de talleres más demandados...</span>
            </td>
        </tr>
    `;

    try {
        const res = await fetch(`${API_BASE_URL}/reports/top-workshops`, {
            headers: { 'Authorization': `Bearer ${AuthState.getToken()}` }
        });

        if (!res.ok) throw new Error('No se pudo generar el reporte.');

        const report = await res.json();

        if (report.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No hay talleres registrados.</td></tr>';
            return;
        }

        tbody.innerHTML = report.map((item, index) => {
            const occupancyPct = Math.round((item.activeEnrollmentsCount / item.maxCapacity) * 100);
            const rankBadge = index === 0 
                ? '<span class="badge bg-warning text-dark fs-6">🥇 1°</span>'
                : index === 1
                ? '<span class="badge bg-secondary fs-6">🥈 2°</span>'
                : index === 2
                ? '<span class="badge bg-danger-subtle text-danger fs-6">🥉 3°</span>'
                : `<span class="badge bg-light text-dark">${index + 1}°</span>`;

            return `
                <tr>
                    <td>${rankBadge}</td>
                    <td><span class="badge badge-code">${item.code}</span></td>
                    <td><strong>${item.title}</strong></td>
                    <td>${item.instructorName}</td>
                    <td class="text-center font-monospace">${item.maxCapacity}</td>
                    <td class="text-center">
                        <span class="badge bg-success fs-6">${item.activeEnrollmentsCount} activos</span>
                    </td>
                    <td class="text-center">
                        <div class="d-flex align-items-center justify-content-center gap-2">
                            <div class="progress flex-grow-1" style="height: 8px; max-width: 100px;">
                                <div class="progress-bar bg-success" role="progressbar" style="width: ${occupancyPct}%"></div>
                            </div>
                            <small class="fw-bold text-dark">${occupancyPct}%</small>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-danger">${err.message}</td></tr>`;
    }
}

// Bulk Excel Import
async function handleExcelImport(event) {
    event.preventDefault();
    clearAlert();

    const fileInput = document.getElementById('excel-file-input');
    const submitBtn = document.getElementById('btn-submit-excel');
    const resultsContainer = document.getElementById('import-results-container');

    if (!fileInput.files || fileInput.files.length === 0) {
        showAlert('Por favor selecciona un archivo Excel.', 'warning');
        return;
    }

    const formData = new FormData();
    formData.append('file', fileInput.files[0]);

    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Procesando filas...';

    try {
        const res = await fetch(`${API_BASE_URL}/enrollments/import`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${AuthState.getToken()}`
            },
            body: formData
        });

        const data = await res.json();

        if (!res.ok) {
            throw new Error(data.error || 'Error al procesar el archivo Excel.');
        }

        showAlert(`Carga completada: ${data.enrolledCount} inscripciones procesadas exitosamente.`, 'success');

        resultsContainer.classList.remove('d-none');
        resultsContainer.innerHTML = `
            <div class="alert alert-info">
                <strong>Resumen de Carga:</strong>
                <ul class="mb-0 mt-2">
                    <li>Filas inscritas exitosamente: <strong>${data.enrolledCount}</strong></li>
                    <li>Filas rechazadas: <strong>${data.rejectedRows ? data.rejectedRows.length : 0}</strong></li>
                </ul>
            </div>
            ${data.rejectedRows && data.rejectedRows.length > 0 ? `
                <h6 class="fw-bold mt-3 text-danger"><i class="bi bi-exclamation-triangle-fill me-1"></i> Detalle de Filas Rechazadas:</h6>
                <div class="table-responsive border rounded-3 bg-light">
                    <table class="table table-sm table-hover mb-0">
                        <thead class="table-secondary">
                            <tr>
                                <th>Fila Excel</th>
                                <th>Motivo del Rechazo</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${data.rejectedRows.map(r => `
                                <tr>
                                    <td><span class="badge bg-danger">Fila ${r.rowNumber}</span></td>
                                    <td class="text-danger small">${r.reason}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            ` : '<div class="alert alert-success mt-2">¡Todas las filas del archivo fueron válidas y procesadas sin errores!</div>'}
        `;

        // Refresh workshops cache
        loadEmployeePanel();
    } catch (err) {
        showAlert(err.message, 'danger');
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i class="bi bi-upload me-1"></i> Procesar Carga Masiva';
    }
}

// Create Workshop (Employee)
async function handleCreateWorkshop(event) {
    event.preventDefault();
    clearAlert();

    const request = {
        code: document.getElementById('ws-code').value.trim(),
        title: document.getElementById('ws-title').value.trim(),
        description: document.getElementById('ws-desc').value.trim(),
        instructorEmail: document.getElementById('ws-instructor-email').value.trim(),
        startDateTime: new Date(document.getElementById('ws-start').value).toISOString(),
        endDateTime: new Date(document.getElementById('ws-end').value).toISOString(),
        maxCapacity: parseInt(document.getElementById('ws-capacity').value, 10)
    };

    try {
        const res = await fetch(`${API_BASE_URL}/workshops`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${AuthState.getToken()}`
            },
            body: JSON.stringify(request)
        });

        const data = await res.json();

        if (!res.ok) {
            throw new Error(data.error || 'Error al crear el taller.');
        }

        // Close modal
        const modalEl = document.getElementById('modalCreateWorkshop');
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();

        showAlert(`¡Taller "${data.title}" (${data.code}) publicado exitosamente!`, 'success');
        document.getElementById('form-create-workshop').reset();

        loadEmployeePanel();
        loadWorkshops();
    } catch (err) {
        showAlert(err.message, 'danger');
    }
}

// ==========================================
// 7. LOGIN & REGISTRATION
// ==========================================

async function handleLogin(event) {
    event.preventDefault();
    clearAlert();

    const email = document.getElementById('login-email').value;
    const password = document.getElementById('login-password').value;

    try {
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Credenciales incorrectas.');
        }

        AuthState.setAuth(data.token, {
            fullName: data.fullName,
            email: data.email,
            role: data.role
        });

        showAlert(`¡Bienvenido de nuevo, ${data.fullName}! (${data.role})`, 'success');

        if (data.role === 'Employee') {
            showSection('employee-panel');
        } else {
            showSection('workshops');
        }
    } catch (err) {
        showAlert(err.message, 'danger');
    }
}

async function handleRegister(event) {
    event.preventDefault();
    clearAlert();

    const fullName = document.getElementById('reg-name').value;
    const email = document.getElementById('reg-email').value;
    const password = document.getElementById('reg-password').value;

    try {
        const response = await fetch(`${API_BASE_URL}/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ fullName, email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Error al registrar el participante.');
        }

        AuthState.setAuth(data.token, {
            fullName: data.fullName,
            email: data.email,
            role: data.role
        });

        showAlert(`¡Registro completado! Te has registrado como participante.`, 'success');
        showSection('workshops');
    } catch (err) {
        showAlert(err.message, 'danger');
    }
}

function handleLogout() {
    AuthState.clear();
    showAlert('Has cerrado sesión correctamente.', 'info');
    showSection('workshops');
}

// Initialize on Load
document.addEventListener('DOMContentLoaded', () => {
    renderNavbar();
    showSection('workshops');
});
