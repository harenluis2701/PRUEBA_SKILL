// SkillHub Client Application - Vanilla JS & Bootstrap
const API_BASE_URL = 'http://localhost:5050/api/v1';

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
    }
};

// UI Navigation Controller
function showSection(sectionName) {
    const sections = ['workshops', 'my-enrollments', 'login', 'register'];
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

    if (AuthState.isAuthenticated()) {
        const user = AuthState.getUser();
        myEnrollmentsLi.classList.remove('d-none');

        navButtons.innerHTML = `
            <span class="navbar-text text-light small me-2">
                <i class="bi bi-person-circle me-1"></i> ${user.fullName} (${user.role})
            </span>
            <button class="btn btn-outline-light btn-sm" onclick="handleLogout()">
                <i class="bi bi-box-arrow-right me-1"></i> Salir
            </button>
        `;
    } else {
        myEnrollmentsLi.classList.add('d-none');
        navButtons.innerHTML = `
            <button class="btn btn-outline-light btn-sm" onclick="showSection('login')">
                <i class="bi bi-box-arrow-in-right me-1"></i> Iniciar Sesión
            </button>
            <button class="btn btn-primary btn-sm fw-semibold" onclick="showSection('register')">
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
            <div class="spinner-border text-primary" role="status"></div>
            <p class="text-muted mt-2">Cargando talleres disponibles...</p>
        </div>
    `;

    try {
        const response = await fetch(`${API_BASE_URL}/workshops`);
        if (!response.ok) throw new Error('No se pudo obtener el catálogo de talleres.');

        const workshops = await response.json();

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
            const spotsClass = w.availableSpots > 0 ? 'bg-success text-white' : 'bg-danger text-white';
            const hasSpots = w.availableSpots > 0;

            return `
                <div class="col-md-6 col-lg-6">
                    <div class="card card-workshop h-100 p-3">
                        <div class="card-body d-flex flex-column">
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <span class="badge badge-code">${w.code}</span>
                                <span class="badge ${spotsClass} badge-spots">
                                    <i class="bi bi-people-fill me-1"></i> ${w.availableSpots} / ${w.maxCapacity} cupos
                                </span>
                            </div>

                            <h5 class="card-title fw-bold text-dark mt-2">${w.title}</h5>
                            <p class="card-text text-muted small flex-grow-1">${w.description}</p>

                            <div class="border-top pt-3 mt-2">
                                <div class="small text-secondary mb-1">
                                    <i class="bi bi-person-badge text-primary me-1"></i> <strong>Instructor:</strong> ${w.instructor.fullName} (${w.instructor.specialty})
                                </div>
                                <div class="small text-secondary mb-3">
                                    <i class="bi bi-clock-history text-primary me-1"></i> <strong>Horario:</strong> ${startDate} - ${endDate}
                                </div>

                                <button class="btn btn-primary w-100 fw-semibold ${!hasSpots ? 'disabled' : ''}" 
                                        onclick="handleEnroll('${w.id}')">
                                    <i class="bi bi-check2-circle me-1"></i> ${hasSpots ? 'Inscribirme al Taller' : 'Sin Cupos Disponibles'}
                                </button>
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

// 2. Handle Enrollment
async function handleEnroll(workshopId) {
    if (!AuthState.isAuthenticated()) {
        showAlert('Debes iniciar sesión para inscribirte en un taller.', 'warning');
        showSection('login');
        return;
    }

    if (!AuthState.isParticipant()) {
        showAlert('Solo los usuarios con rol Participante pueden inscribirse en talleres.', 'danger');
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

// 3. Load My Enrollments
async function loadMyEnrollments() {
    clearAlert();
    const tbody = document.getElementById('my-enrollments-body');
    tbody.innerHTML = `
        <tr>
            <td colspan="6" class="text-center py-4">
                <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
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
                                <button class="btn btn-outline-primary" title="Descargar Constancia PDF" onclick="handleDownloadCertificate('${e.id}')">
                                    <i class="bi bi-file-earmark-pdf me-1"></i> PDF
                                </button>
                            ` : `
                                <span class="text-muted small italic">Sin acciones</span>
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
        loadMyEnrollments();
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

// 6. Login Handler
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

        showAlert(`¡Bienvenido de nuevo, ${data.fullName}!`, 'success');
        showSection('workshops');
    } catch (err) {
        showAlert(err.message, 'danger');
    }
}

// 7. Register Handler
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

// 8. Logout Handler
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
