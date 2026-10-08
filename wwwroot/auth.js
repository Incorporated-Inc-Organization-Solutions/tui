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

window.tuiInvoices = {
    async loadForm() {
        const select = document.getElementById('recipient-id');
        const form = select?.closest('form');
        if (!select || !form) return;

        const response = await fetch('/api/recipients', { credentials: 'same-origin' });
        if (!response.ok) {
            form.querySelector('[data-form-error]').textContent = await tuiAuth.problemMessage(response);
            select.replaceChildren(new Option('Ontvangers konden niet worden geladen.', ''));
            return;
        }

        const recipients = await response.json();
        select.replaceChildren(new Option('Kies een ontvanger', ''));
        for (const recipient of recipients) {
            select.add(new Option(recipient.name, recipient.id));
        }
    },

    async create(event) {
        event.preventDefault();
        const form = event.currentTarget;
        this.clearErrors(form);
        const formData = new FormData(form);
        const amount = String(formData.get('totalAmount') || '').replace(',', '.');
        const payload = {
            recipientId: Number(formData.get('recipientId')),
            invoiceDate: formData.get('invoiceDate'),
            description: formData.get('description'),
            totalAmount: Number(amount)
        };

        if (!Number.isFinite(payload.totalAmount)) {
            this.showFieldError(form, 'TotalAmount', 'Vul een geldig totaalbedrag in.');
            return false;
        }

        try {
            const response = await fetch('/api/invoices', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                const invoice = await response.json();
                window.location.assign(`/invoices/${encodeURIComponent(invoice.internalReference)}`);
                return false;
            }

            this.showProblem(form, await this.readProblem(response));
        } catch {
            form.querySelector('[data-form-error]').textContent = 'De server is tijdelijk niet bereikbaar.';
        }

        return false;
    },

    async loadList() {
        const body = document.getElementById('invoices-table-body');
        const errorElement = document.getElementById('invoices-error');
        if (!body || !errorElement) return;

        const response = await fetch('/api/invoices', { credentials: 'same-origin' });
        if (!response.ok) {
            errorElement.textContent = await tuiAuth.problemMessage(response);
            return;
        }

        const invoices = await response.json();
        body.replaceChildren(...invoices.map(invoice => this.invoiceRow(invoice)));
    },

    invoiceRow(invoice) {
        const row = document.createElement('tr');
        const referenceCell = document.createElement('td');
        const link = document.createElement('a');
        link.href = `/invoices/${encodeURIComponent(invoice.internalReference)}`;
        link.textContent = invoice.internalReference;
        referenceCell.appendChild(link);
        row.appendChild(referenceCell);

        for (const value of [invoice.recipientName, this.formatDate(invoice.invoiceDate), this.formatCurrency(invoice.totalAmount), invoice.paymentStatus]) {
            const cell = document.createElement('td');
            cell.textContent = value;
            row.appendChild(cell);
        }
        return row;
    },

    async loadDetail(internalReference) {
        const errorElement = document.getElementById('invoice-detail-error');
        const detailList = document.getElementById('invoice-details');
        const response = await fetch(`/api/invoices/${encodeURIComponent(internalReference)}`, { credentials: 'same-origin' });
        if (!response.ok) {
            errorElement.textContent = await tuiAuth.problemMessage(response);
            return;
        }

        const invoice = await response.json();
        document.getElementById('invoice-reference').textContent = invoice.internalReference;
        document.getElementById('invoice-recipient').textContent = invoice.recipientName;
        document.getElementById('invoice-date-value').textContent = this.formatDate(invoice.invoiceDate);
        document.getElementById('invoice-description').textContent = invoice.description;
        document.getElementById('invoice-total-amount').textContent = this.formatCurrency(invoice.totalAmount);
        document.getElementById('invoice-status').textContent = invoice.paymentStatus;
        detailList.hidden = false;
    },

    clearErrors(form) {
        form.querySelector('[data-form-error]').textContent = '';
        form.querySelectorAll('[data-field-error]').forEach(element => element.textContent = '');
    },

    showProblem(form, problem) {
        if (problem.errors) {
            for (const [field, messages] of Object.entries(problem.errors)) {
                this.showFieldError(form, field, messages.join(' '));
            }
            return;
        }

        form.querySelector('[data-form-error]').textContent = problem.detail || problem.title || 'De aanvraag is mislukt.';
    },

    showFieldError(form, field, message) {
        const errorElement = form.querySelector(`[data-field-error="${field}"]`);
        if (errorElement) {
            errorElement.textContent = message;
        } else {
            form.querySelector('[data-form-error]').textContent = message;
        }
    },

    async readProblem(response) {
        try {
            return await response.json();
        } catch {
            return { title: 'De aanvraag is mislukt.' };
        }
    },

    formatDate(value) {
        return new Intl.DateTimeFormat('nl-NL').format(new Date(`${value}T00:00:00`));
    },

    formatCurrency(value) {
        return new Intl.NumberFormat('nl-NL', { style: 'currency', currency: 'EUR' }).format(value);
    }
};
