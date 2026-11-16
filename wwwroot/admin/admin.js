
"use strict";

// =========================================
// HUNTER GRASS SERVICES ADMIN DASHBOARD
// LIVE SQLITE QUOTE REQUESTS
// =========================================

// =========================================
// APPLICATION STATE
// =========================================

// Quotes are loaded from the protected API.
// No fictional customer records are used.

let customerQuotes = [];
let selectedQuote = null;

// Current QuoteRequest model does not yet
// contain a persistent Status property.
// Display all existing records as New.

const DEFAULT_STATUS = "New";

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

    // Display dates in the browser's local time.
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
    const tableBody = getElement("quotesTable");

    // Show progress before requesting data.
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

        // Administrator session has expired.
        if (response.status === 401) {
            window.location.replace("/admin/login/");
            return;
        }

        // Authenticated but not authorized.
        if (response.status === 403) {
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

        // Convert API records to dashboard records.
        customerQuotes = data.map(quote => ({
            id: quote.id,
            customerName: quote.customerName ?? "",
            email: quote.email ?? "",
            phone: quote.phone ?? "",
            address: quote.address ?? "",
            service: quote.service ?? "",
            message: quote.message ?? "",
            createdAt: quote.createdAt,
            status: DEFAULT_STATUS
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

    // Until database status support is added,
    // all retrieved requests are treated as New.
    const newCount = total;
    const completedCount = 0;

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
        const message =
            customerQuotes.length === 0
                ? "No customer quote requests have been submitted yet."
                : "No matching quote requests found.";

        showTableMessage(message);
        return;
    }

    filteredQuotes.forEach(quote => {
        const row = document.createElement("tr");

        // Customer name, service, and date.
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

        // Status badge.
        const statusCell = document.createElement("td");
        const badge = document.createElement("span");

        badge.textContent = quote.status;
        badge.classList.add(
            "status-badge",
            "status-new"
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

    // Status editing is unavailable until
    // the database supports persistent statuses.
    const statusSelect = getElement("updateStatus");
    const saveButton = getElement("saveStatus");

    statusSelect.value = DEFAULT_STATUS;
    statusSelect.disabled = true;

    saveButton.disabled = true;

    setText(
        "statusUpdateMessage",
        "Status editing will be available after database status management is added."
    );

    getElement("quoteDetailsDialog").showModal();
}

// =========================================
// CLOSE QUOTE DETAILS
// =========================================

function closeQuoteDetails() {
    getElement("quoteDetailsDialog").close();
}

// =========================================
// LOGOUT STATUS MESSAGES
// =========================================

function displayLogoutMessage(message, type = "") {
    const element = getElement("logoutMessage");

    element.textContent = message;
    element.className = "logout-message";

    if (type) {
        element.classList.add(type);
    }
}

// =========================================
// RETRIEVE ANTIFORGERY TOKEN
// =========================================

async function getLogoutAntiforgeryToken() {
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
        const token =
            await getLogoutAntiforgeryToken();

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

            window.location.replace(
                "/admin/login/"
            );

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
    // Prevent demo-only controls from modifying
    // the appearance of live customer data.
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

    // Disable status editing until Step 33.
    getElement("updateStatus").disabled = true;
    getElement("saveStatus").disabled = true;

    // Status filter.
    getElement("statusFilter").addEventListener(
        "change",
        displayQuotes
    );

    // Customer search.
    getElement("searchInput").addEventListener(
        "input",
        displayQuotes
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

    // Load actual customer records from SQLite.
    loadCustomerQuotes();
}

// =========================================
// START DASHBOARD
// =========================================

document.addEventListener(
    "DOMContentLoaded",
    initializeDashboard
);
