
"use strict";

// =========================================
// HUNTER GRASS SERVICES ADMIN DASHBOARD
// DEMO DATA ONLY
// =========================================

// =========================================
// CONFIGURATION
// =========================================

const STORAGE_KEY = "hunterGrassDemoStatuses";

const validStatuses = [
    "New",
    "Contacted",
    "Completed"
];

// =========================================
// ORIGINAL SAMPLE QUOTE REQUESTS
// =========================================

const originalQuotes = [
    {
        id: 1,
        customerName: "John Test",
        email: "john@example.com",
        phone: "555-0100",
        address: "123 Example Street",
        service: "Lawn Mowing",
        date: "2026-11-10",
        status: "New",
        message: "I would like a quote for weekly lawn mowing."
    },
    {
        id: 2,
        customerName: "Jane Sample",
        email: "jane@example.com",
        phone: "555-0101",
        address: "456 Sample Avenue",
        service: "Mulching",
        date: "2026-11-09",
        status: "Completed",
        message: "I need fresh mulch around my flower beds."
    },
    {
        id: 3,
        customerName: "Alex Demo",
        email: "alex@example.com",
        phone: "555-0102",
        address: "789 Demo Lane",
        service: "Seasonal Cleanup",
        date: "2026-11-08",
        status: "New",
        message: "I need help removing leaves from my yard."
    },
    {
        id: 4,
        customerName: "Taylor Example",
        email: "taylor@example.com",
        phone: "555-0103",
        address: "321 Example Drive",
        service: "Edging & Trimming",
        date: "2026-11-07",
        status: "Contacted",
        message: "I would like edging along my driveway."
    }
];

// Working copy of sample quotes

let sampleQuotes = originalQuotes.map(quote => ({
    ...quote
}));

// Currently selected quote

let selectedQuote = null;


// =========================================
// LOAD SAVED DEMO STATUSES
// =========================================

function loadSavedStatuses() {

    try {

        const savedData =
            localStorage.getItem(STORAGE_KEY);

        if (!savedData) {
            return;
        }

        const savedStatuses =
            JSON.parse(savedData);

        if (
            !savedStatuses ||
            typeof savedStatuses !== "object" ||
            Array.isArray(savedStatuses)
        ) {
            return;
        }

        sampleQuotes.forEach(quote => {

            const savedStatus =
                savedStatuses[quote.id];

            if (validStatuses.includes(savedStatus)) {

                quote.status = savedStatus;
            }
        });

    } catch (error) {

        console.error(
            "Unable to load saved demo statuses:",
            error
        );
    }
}


// =========================================
// SAVE DEMO STATUSES
// =========================================

function saveStatusesToStorage() {

    const statuses = {};

    sampleQuotes.forEach(quote => {

        statuses[quote.id] = quote.status;
    });

    try {

        localStorage.setItem(
            STORAGE_KEY,
            JSON.stringify(statuses)
        );

        return true;

    } catch (error) {

        console.error(
            "Unable to save demo statuses:",
            error
        );

        return false;
    }
}


// =========================================
// UPDATE DASHBOARD STATISTICS
// =========================================

function updateStatistics() {

    const totalQuotes = sampleQuotes.length;

    const newQuotes = sampleQuotes.filter(
        quote => quote.status === "New"
    ).length;

    const completedQuotes = sampleQuotes.filter(
        quote => quote.status === "Completed"
    ).length;

    document.getElementById("totalQuotes").textContent =
        totalQuotes;

    document.getElementById("newQuotes").textContent =
        newQuotes;

    document.getElementById("completedQuotes").textContent =
        completedQuotes;
}


// =========================================
// DISPLAY QUOTE REQUESTS
// =========================================

function displayQuotes() {

    const tableBody =
        document.getElementById("quotesTable");

    const statusFilter =
        document.getElementById("statusFilter");

    const searchInput =
        document.getElementById("searchInput");

    const selectedStatus = statusFilter.value;

    const searchTerm =
        searchInput.value.trim().toLowerCase();

    // Filter by status and customer search

    const filteredQuotes = sampleQuotes.filter(quote => {

        const matchesStatus =
            selectedStatus === "All" ||
            quote.status === selectedStatus;

        const matchesSearch =
            quote.customerName.toLowerCase()
                .includes(searchTerm) ||
            quote.service.toLowerCase()
                .includes(searchTerm);

        return matchesStatus && matchesSearch;
    });

    // Clear existing table rows

    tableBody.replaceChildren();

    // Display message when no quotes match

    if (filteredQuotes.length === 0) {

        const row = document.createElement("tr");

        const cell = document.createElement("td");

        cell.colSpan = 5;

        cell.classList.add("no-results");

        cell.textContent =
            "No matching quote requests found.";

        row.appendChild(cell);

        tableBody.appendChild(row);

        return;
    }

    // Create table rows

    filteredQuotes.forEach(quote => {

        const row = document.createElement("tr");

        // Customer, Service, and Date

        const values = [
            quote.customerName,
            quote.service,
            quote.date
        ];

        values.forEach(value => {

            const cell = document.createElement("td");

            cell.textContent = value;

            row.appendChild(cell);
        });

        // Status Badge

        const statusCell =
            document.createElement("td");

        const statusBadge =
            document.createElement("span");

        statusBadge.textContent = quote.status;

        statusBadge.classList.add(
            "status-badge",
            `status-${quote.status.toLowerCase()}`
        );

        statusCell.appendChild(statusBadge);

        row.appendChild(statusCell);

        // View Details Button

        const actionsCell =
            document.createElement("td");

        const viewButton =
            document.createElement("button");

        viewButton.type = "button";

        viewButton.textContent = "View Details";

        viewButton.classList.add(
            "view-details-button"
        );

        viewButton.setAttribute(
            "aria-label",
            `View details for ${quote.customerName}`
        );

        viewButton.addEventListener("click", () => {

            showQuoteDetails(quote);
        });

        actionsCell.appendChild(viewButton);

        row.appendChild(actionsCell);

        // Add completed row to table

        tableBody.appendChild(row);
    });
}


// =========================================
// SHOW QUOTE DETAILS POPUP
// =========================================

function showQuoteDetails(quote) {

    selectedQuote = quote;

    document.getElementById("detailId").textContent =
        quote.id;

    document.getElementById("detailCustomer").textContent =
        quote.customerName;

    document.getElementById("detailEmail").textContent =
        quote.email;

    document.getElementById("detailPhone").textContent =
        quote.phone;

    document.getElementById("detailAddress").textContent =
        quote.address;

    document.getElementById("detailService").textContent =
        quote.service;

    document.getElementById("detailDate").textContent =
        quote.date;

    document.getElementById("detailStatus").textContent =
        quote.status;

    document.getElementById("detailMessage").textContent =
        quote.message;

    // Set dropdown to current status

    document.getElementById("updateStatus").value =
        quote.status;

    // Clear previous confirmation message

    document.getElementById("statusUpdateMessage")
        .textContent = "";

    // Open popup

    const dialog =
        document.getElementById("quoteDetailsDialog");

    dialog.showModal();
}


// =========================================
// UPDATE QUOTE STATUS
// =========================================

function saveQuoteStatus() {

    if (!selectedQuote) {
        return;
    }

    const newStatus =
        document.getElementById("updateStatus").value;

    const message =
        document.getElementById("statusUpdateMessage");

    // Validate selected status

    if (!validStatuses.includes(newStatus)) {

        message.textContent =
            "Please select a valid status.";

        return;
    }

    // Check whether status changed

    if (selectedQuote.status === newStatus) {

        message.textContent =
            "No changes were made.";

        return;
    }

    // Update selected quote

    selectedQuote.status = newStatus;

    // Save to browser storage

    const saved = saveStatusesToStorage();

    // Update popup status

    document.getElementById("detailStatus").textContent =
        newStatus;

    // Refresh dashboard

    updateStatistics();

    displayQuotes();

    // Display confirmation

    if (saved) {

        message.textContent =
            `Quote status updated to ${newStatus} and saved.`;

    } else {

        message.textContent =
            "Status updated, but browser storage is unavailable.";
    }
}


// =========================================
// RESET DEMO DATA
// =========================================

function resetDemoData() {

    const confirmed = window.confirm(
        "Reset all demo quote statuses to their original values?"
    );

    if (!confirmed) {
        return;
    }

    try {

        localStorage.removeItem(STORAGE_KEY);

    } catch (error) {

        console.error(
            "Unable to clear saved demo statuses:",
            error
        );

        window.alert(
            "Unable to reset browser storage."
        );

        return;
    }

    // Restore original sample quotes

    sampleQuotes = originalQuotes.map(quote => ({
        ...quote
    }));

    selectedQuote = null;

    // Reset filter and search

    document.getElementById("statusFilter").value =
        "All";

    document.getElementById("searchInput").value =
        "";

    // Refresh dashboard

    updateStatistics();

    displayQuotes();

    window.alert(
        "Demo data has been reset successfully."
    );
}


// =========================================
// CLOSE QUOTE DETAILS POPUP
// =========================================

function closeQuoteDetails() {

    const dialog =
        document.getElementById("quoteDetailsDialog");

    dialog.close();
}


// =========================================
// ADMINISTRATOR LOGOUT
// =========================================

// Display logout status messages

function displayLogoutMessage(message, type = "") {

    const logoutMessage =
        document.getElementById("logoutMessage");

    logoutMessage.textContent = message;

    logoutMessage.className = "logout-message";

    if (type) {

        logoutMessage.classList.add(type);
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
// SIGN OUT ADMINISTRATOR
// =========================================

async function handleLogout() {

    const logoutButton =
        document.getElementById("logoutButton");

    // Prevent duplicate logout requests

    if (logoutButton.disabled) {
        return;
    }

    logoutButton.disabled = true;

    logoutButton.textContent = "Signing Out...";

    displayLogoutMessage("Signing out...");

    let redirecting = false;

    try {

        // Retrieve fresh antiforgery token

        const token =
            await getLogoutAntiforgeryToken();

        // Send logout request

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

        // Successful logout or expired session

        if (
            response.ok ||
            response.status === 401
        ) {

            displayLogoutMessage(
                "Signed out successfully.",
                "success"
            );

            redirecting = true;

            // Replace dashboard in browser history

            window.location.replace(
                "/admin/login/"
            );

            return;
        }

        // Invalid antiforgery token

        if (response.status === 400) {

            displayLogoutMessage(
                "Security validation failed. Please try again.",
                "error"
            );

            return;
        }

        // Other unsuccessful responses

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

        // Restore button if logout was unsuccessful

        if (!redirecting) {

            logoutButton.disabled = false;

            logoutButton.textContent = "Sign Out";
        }
    }
}


// =========================================
// INITIALIZE DASHBOARD
// =========================================

function initializeDashboard() {

    // Restore saved demo statuses

    loadSavedStatuses();

    // Display dashboard

    updateStatistics();

    displayQuotes();

    // Status Filter

    document.getElementById("statusFilter")
        .addEventListener(
            "change",
            displayQuotes
        );

    // Customer Search

    document.getElementById("searchInput")
        .addEventListener(
            "input",
            displayQuotes
        );

    // Save Quote Status

    document.getElementById("saveStatus")
        .addEventListener(
            "click",
            saveQuoteStatus
        );

    // Close Popup - X Button

    document.getElementById("closeDetails")
        .addEventListener(
            "click",
            closeQuoteDetails
        );

    // Close Popup - Footer Button

    document.getElementById("closeDetailsFooter")
        .addEventListener(
            "click",
            closeQuoteDetails
        );

    // Reset selected quote when popup closes

    document.getElementById("quoteDetailsDialog")
        .addEventListener("close", () => {

            selectedQuote = null;
        });

    // Reset Demo Button

    const resetButton =
        document.getElementById("resetDemo");

    if (resetButton) {

        resetButton.addEventListener(
            "click",
            resetDemoData
        );
    }

    // Administrator Sign Out Button

    const logoutButton =
        document.getElementById("logoutButton");

    if (logoutButton) {

        logoutButton.addEventListener(
            "click",
            handleLogout
        );
    }
}


// =========================================
// START DASHBOARD
// =========================================

document.addEventListener(
    "DOMContentLoaded",
    initializeDashboard
);
