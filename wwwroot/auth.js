window.tuiAuth = {
    async submit(event, endpoint, redirectUrl) {
        event.preventDefault();
        const form = event.currentTarget;
        const errorElement = form.querySelector('[data-form-error]');
        errorElement.textContent = '';

        const payload = Object.fromEntries(new FormData(form).entries());
        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                window.location.assign(redirectUrl);
                return false;
            }

            errorElement.textContent = await this.problemMessage(response);
        } catch {
            errorElement.textContent = 'De server is tijdelijk niet bereikbaar.';
        }

        return false;
    },

    async logout(event) {
        event.preventDefault();
        await fetch('/api/auth/logout', { method: 'POST', credentials: 'same-origin' });
        window.location.assign('/login');
        return false;
    },

    async problemMessage(response) {
        try {
            const problem = await response.json();
            if (problem.errors) {
                return Object.values(problem.errors).flat().join(' ');
            }

            return problem.detail || problem.title || 'De aanvraag is mislukt.';
        } catch {
            return 'De aanvraag is mislukt.';
        }
    }
};

window.tuiAdmin = {
    async loadUsers() {
        const body = document.getElementById('users-table-body');
        const errorElement = document.getElementById('users-error');
        if (!body || !errorElement) return;

        const response = await fetch('/api/users', { credentials: 'same-origin' });
        if (!response.ok) {
            errorElement.textContent = await tuiAuth.problemMessage(response);
            return;
        }

        const users = await response.json();
        body.replaceChildren(...users.map(user => this.userRow(user, errorElement)));
    },

    userRow(user, errorElement) {
        const row = document.createElement('tr');
        for (const value of [user.username, user.email, new Date(user.createdAt).toLocaleString('nl-NL')]) {
            const cell = document.createElement('td');
            cell.textContent = value;
            row.appendChild(cell);
        }

        const roleCell = document.createElement('td');
        const select = document.createElement('select');
        for (const role of ['User', 'Admin']) {
            const option = new Option(role === 'Admin' ? 'Admin' : 'Gebruiker', role, false, user.role === role);
            select.add(option);
        }
        roleCell.appendChild(select);
        row.appendChild(roleCell);

        const actionCell = document.createElement('td');
        const button = document.createElement('button');
        button.type = 'button';
        button.textContent = 'Opslaan';
        button.addEventListener('click', async () => {
            errorElement.textContent = '';
            const response = await fetch(`/api/users/${user.id}/role`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify({ role: select.value })
            });
            if (!response.ok) {
                errorElement.textContent = await tuiAuth.problemMessage(response);
            }
        });
        actionCell.appendChild(button);
        row.appendChild(actionCell);
        return row;
    }
};
