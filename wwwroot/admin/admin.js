
"use strict";

// =========================================
// HUNTER GRASS SERVICES ADMIN DASHBOARD
// LIVE SQLITE QUOTE REQUESTS
// =========================================

// =========================================
// APPLICATION STATE
// =========================================

let customerQuotes = [];
let selectedQuote = null;
let savingStatus = false;

const ALLOWED_STATUSES = [
    "New",
    "Contacted",
    "Completed"
];

// =========================================
// DOM HELPERS
// =========================================

function getElement(id) {
    return document.getElementById(id);
}

function setText(id, value) {
    const element = getElement(id);

    if (element) {
        element.textContent = value ?? "";
    }
}

function showStatusMessage(message, type = "") {
    const element = getElement("statusUpdateMessage");

    if (!element) {
        return;
    }

    element.textContent = message;
    element.className = "status-update-message";

    if (type) {
        element.classList.add(type);
    }
}

// =========================================
// DATE FORMATTING
// =========================================

function formatQuoteDate(createdAt) {
    if (!createdAt) {
        return "Not available";
    }

    const date = new Date(createdAt);

    if (Number.isNaN(date.getTime())) {
        return "Not available";
    }

    return date.toLocaleDateString(undefined, {
        year: "numeric",
        month: "2-digit",
        day: "2-digit"
    });
}

// =========================================
// LOAD REAL CUSTOMER QUOTES
// =========================================

async function loadCustomerQuotes() {
    showTableMessage("Loading customer requests...");

    try {
        const response = await fetch(
            "/api/admin/quotes",
            {
                method: "GET",
                credentials: "same-origin",
                cache: "no-store"
            }
        );

        if (response.status === 401) {
            window.location.replace("/admin/login/");
            return;
        }

        if (response.status === 403) {
            customerQuotes = [];
            updateStatistics();

            showTableMessage(
                "You do not have permission to view customer requests."
            );
            return;
        }

        if (!response.ok) {
            throw new Error(
                `Unable to load quotes. HTTP ${response.status}`
            );
        }

        const data = await response.json();

        if (!Array.isArray(data)) {
            throw new Error(
                "Unexpected response from the quotes API."
            );
        }

        customerQuotes = data.map(quote => ({
            id: quote.id,
            customerName: quote.customerName ?? "",
            email: quote.email ?? "",
            phone: quote.phone ?? "",
            address: quote.address ?? "",
            service: quote.service ?? "",
            message: quote.message ?? "",
            createdAt: quote.createdAt,
            status: quote.status ?? "New"
        }));

        updateStatistics();
        displayQuotes();

    } catch (error) {
        console.error(
            "Unable to load customer quotes:",
            error
        );

        customerQuotes = [];
        updateStatistics();

        showTableMessage(
            "Unable to load customer requests. Please refresh the page."
        );
    }
}

// =========================================
// TABLE STATUS MESSAGES
// =========================================

function showTableMessage(message) {
    const tableBody = getElement("quotesTable");

    if (!tableBody) {
        return;
    }

    tableBody.replaceChildren();

    const row = document.createElement("tr");
    const cell = document.createElement("td");

    cell.colSpan = 5;
    cell.classList.add("no-results");
    cell.textContent = message;

    row.appendChild(cell);
    tableBody.appendChild(row);
}

// =========================================
// DASHBOARD STATISTICS
// =========================================

function updateStatistics() {
    const total = customerQuotes.length;

    const newCount = customerQuotes.filter(
        quote => quote.status === "New"
    ).length;

    const completedCount = customerQuotes.filter(
        quote => quote.status === "Completed"
    ).length;

    setText("totalQuotes", total);
    setText("newQuotes", newCount);
    setText("completedQuotes", completedCount);
}

// =========================================
// DISPLAY CUSTOMER QUOTES
// =========================================

function displayQuotes() {
    const tableBody = getElement("quotesTable");

    const selectedStatus =
        getElement("statusFilter").value;

    const searchTerm =
        getElement("searchInput")
            .value
            .trim()
            .toLowerCase();

    const filteredQuotes = customerQuotes.filter(quote => {
        const matchesStatus =
            selectedStatus === "All" ||
            quote.status === selectedStatus;

        const matchesSearch =
            quote.customerName
                .toLowerCase()
                .includes(searchTerm) ||
            quote.service
                .toLowerCase()
                .includes(searchTerm);

        return matchesStatus && matchesSearch;
    });

    tableBody.replaceChildren();

    if (filteredQuotes.length === 0) {
        showTableMessage(
            customerQuotes.length === 0
                ? "No customer quote requests have been submitted yet."
                : "No matching quote requests found."
        );
        return;
    }

    filteredQuotes.forEach(quote => {
        const row = document.createElement("tr");

        const values = [
            quote.customerName,
            quote.service,
            formatQuoteDate(quote.createdAt)
        ];

        values.forEach(value => {
            const cell = document.createElement("td");
            cell.textContent = value;
            row.appendChild(cell);
        });

        // Display status from SQLite.
        const statusCell = document.createElement("td");
        const badge = document.createElement("span");

        badge.textContent = quote.status;

        const statusClass =
            ALLOWED_STATUSES.includes(quote.status)
                ? `status-${quote.status.toLowerCase()}`
                : "status-new";

        badge.classList.add(
            "status-badge",
            statusClass
        );

        statusCell.appendChild(badge);
        row.appendChild(statusCell);

        // View Details button.
        const actionsCell = document.createElement("td");
        const button = document.createElement("button");

        button.type = "button";
        button.textContent = "View Details";
        button.classList.add("view-details-button");

        button.setAttribute(
            "aria-label",
            `View details for ${quote.customerName}`
        );

        button.addEventListener("click", () => {
            showQuoteDetails(quote);
        });

        actionsCell.appendChild(button);
        row.appendChild(actionsCell);

        tableBody.appendChild(row);
    });
}

// =========================================
// VIEW CUSTOMER QUOTE DETAILS
// =========================================

function showQuoteDetails(quote) {
    selectedQuote = quote;

    setText("detailId", quote.id);
    setText("detailCustomer", quote.customerName);
    setText("detailEmail", quote.email);
    setText("detailPhone", quote.phone);
    setText("detailAddress", quote.address);
    setText("detailService", quote.service);

    setText(
        "detailDate",
        formatQuoteDate(quote.createdAt)
    );

    setText("detailStatus", quote.status);

    setText(
        "detailMessage",
        quote.message || "No additional message provided."
    );

    const statusSelect = getElement("updateStatus");
    const saveButton = getElement("saveStatus");

    statusSelect.value = quote.status;
    statusSelect.disabled = false;
    saveButton.disabled = false;

    showStatusMessage(
        "Select a status and click Save Status."
    );

    getElement("quoteDetailsDialog").showModal();
}

// =========================================
// SAVE QUOTE STATUS TO SQLITE
// =========================================

async function saveQuoteStatus() {
    if (!selectedQuote || savingStatus) {
        return;
    }

    const statusSelect = getElement("updateStatus");
    const saveButton = getElement("saveStatus");

    const requestedStatus = statusSelect.value;

    if (!ALLOWED_STATUSES.includes(requestedStatus)) {
        showStatusMessage(
            "Please select a valid status.",
            "error"
        );
        return;
    }

    if (requestedStatus === selectedQuote.status) {
        showStatusMessage(
            "This quote already has the selected status."
        );
        return;
    }

    savingStatus = true;

    statusSelect.disabled = true;
    saveButton.disabled = true;
    saveButton.textContent = "Saving...";

    showStatusMessage("Saving status...");

    try {
        // Obtain a fresh antiforgery token.
        const token = await getAntiforgeryToken();

        const response = await fetch(
            `/api/admin/quotes/${selectedQuote.id}/status`,
            {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": token
                },
                credentials: "same-origin",
                cache: "no-store",
                body: JSON.stringify({
                    status: requestedStatus
                })
            }
        );

        if (response.status === 401) {
            window.location.replace("/admin/login/");
            return;
        }

        if (response.status === 403) {
            showStatusMessage(
                "You do not have permission to update quotes.",
                "error"
            );
            return;
        }

        if (!response.ok) {
            let message = "Unable to save quote status.";

            try {
                const errorData = await response.json();

                if (errorData.message) {
                    message = errorData.message;
                }
            } catch {
                // Keep the default error message.
            }

            throw new Error(message);
        }

        const result = await response.json();

        if (!ALLOWED_STATUSES.includes(result.status)) {
            throw new Error(
                "The server returned an unexpected status."
            );
        }

        // Update the in-memory quote only after
        // the server confirms successful saving.
        selectedQuote.status = result.status;

        setText("detailStatus", result.status);
        statusSelect.value = result.status;

        updateStatistics();
        displayQuotes();

        showStatusMessage(
            "Quote status saved successfully.",
            "success"
        );

    } catch (error) {
        console.error(
            "Unable to update quote status:",
            error
        );

        showStatusMessage(
            error.message || "Unable to save quote status.",
            "error"
        );

    } finally {
        savingStatus = false;
        statusSelect.disabled = false;
        saveButton.disabled = false;
        saveButton.textContent = "Save Status";
    }
}

// =========================================
// CLOSE QUOTE DETAILS
// =========================================

function closeQuoteDetails() {
    if (savingStatus) {
        return;
    }

    getElement("quoteDetailsDialog").close();
}

// =========================================
// RETRIEVE ANTIFORGERY TOKEN
// =========================================

async function getAntiforgeryToken() {
    const response = await fetch(
        "/api/admin/antiforgery",
        {
            method: "GET",
            credentials: "same-origin",
            cache: "no-store"
        }
    );

    if (!response.ok) {
        throw new Error(
            "Unable to retrieve security token."
        );
    }

    const data = await response.json();

    if (!data.token) {
        throw new Error(
            "Security token was not received."
        );
    }

    return data.token;
}

// =========================================
// LOGOUT STATUS MESSAGES
// =========================================

function displayLogoutMessage(message, type = "") {
    const element = getElement("logoutMessage");

    if (!element) {
        return;
    }

    element.textContent = message;
    element.className = "logout-message";

    if (type) {
        element.classList.add(type);
    }
}

// =========================================
// SECURE ADMINISTRATOR LOGOUT
// =========================================

async function handleLogout() {
    const logoutButton = getElement("logoutButton");

    if (logoutButton.disabled) {
        return;
    }

    logoutButton.disabled = true;
    logoutButton.textContent = "Signing Out...";

    displayLogoutMessage("Signing out...");

    let redirecting = false;

    try {
        const token = await getAntiforgeryToken();

        const response = await fetch(
            "/api/admin/logout",
            {
                method: "POST",
                headers: {
                    "X-CSRF-TOKEN": token
                },
                credentials: "same-origin",
                cache: "no-store"
            }
        );

        if (response.ok || response.status === 401) {
            displayLogoutMessage(
                "Signed out successfully.",
                "success"
            );

            redirecting = true;

            window.location.replace("/admin/login/");
            return;
        }

        if (response.status === 400) {
            displayLogoutMessage(
                "Security validation failed. Please try again.",
                "error"
            );
            return;
        }

        displayLogoutMessage(
            "Unable to sign out. Please try again.",
            "error"
        );

    } catch (error) {
        console.error(
            "Administrator logout error:",
            error
        );

        displayLogoutMessage(
            "Unable to connect to the server. Please try again.",
            "error"
        );

    } finally {
        if (!redirecting) {
            logoutButton.disabled = false;
            logoutButton.textContent = "Sign Out";
        }
    }
}

// =========================================
// INITIALIZE LIVE DASHBOARD
// =========================================

function initializeDashboard() {
    // Hide legacy demo controls, if present.
    const resetButton = getElement("resetDemo");

    if (resetButton) {
        resetButton.hidden = true;
        resetButton.disabled = true;
    }

    const demoLabel =
        document.querySelector(".demo-label");

    if (demoLabel) {
        demoLabel.textContent = "Live Data";
    }

    // Status filtering.
    getElement("statusFilter").addEventListener(
        "change",
        displayQuotes
    );

    // Customer searching.
    getElement("searchInput").addEventListener(
        "input",
        displayQuotes
    );

    // Save quote status.
    getElement("saveStatus").addEventListener(
        "click",
        saveQuoteStatus
    );

    // Quote details dialog.
    getElement("closeDetails").addEventListener(
        "click",
        closeQuoteDetails
    );

    getElement("closeDetailsFooter").addEventListener(
        "click",
        closeQuoteDetails
    );

    getElement("quoteDetailsDialog").addEventListener(
        "close",
        () => {
            selectedQuote = null;
        }
    );

    // Administrator logout.
    const logoutButton = getElement("logoutButton");

    if (logoutButton) {
        logoutButton.addEventListener(
            "click",
            handleLogout
        );
    }

    // Load customer quotes from SQLite.
    loadCustomerQuotes();
}

// =========================================
// START DASHBOARD
// =========================================

document.addEventListener(
    "DOMContentLoaded",
    initializeDashboard
);
