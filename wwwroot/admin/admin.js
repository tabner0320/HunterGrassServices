
"use strict";

// =========================================
// Hunter Grass Services Admin Dashboard
// DEMO DATA ONLY - No real customer information
// =========================================

// Local Storage Key

const STORAGE_KEY = "hunterGrassDemoStatuses";

// Original Sample Quote Requests

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

// Valid Quote Statuses

const validStatuses = [
    "New",
    "Contacted",
    "Completed"
];

// Working copy of sample quotes

let sampleQuotes = originalQuotes.map(quote => ({
    ...quote
}));

// Currently selected quote

let selectedQuote = null;

// =========================================
// Load Saved Quote Statuses
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
// Save Quote Statuses to Local Storage
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
// Update Dashboard Statistics
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
// Display Quote Requests
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

    // Display empty search message

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
// Show Quote Details Popup
// =========================================

function showQuoteDetails(quote) {

    // Remember the selected quote

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

    // Open the popup

    const dialog =
        document.getElementById("quoteDetailsDialog");

    dialog.showModal();
}

// =========================================
// Update Quote Request Status
// =========================================

function saveQuoteStatus() {

    // Make sure a quote is selected

    if (!selectedQuote) {
        return;
    }

    const newStatus =
        document.getElementById("updateStatus").value;

    const message =
        document.getElementById("statusUpdateMessage");

    // Validate the selected status

    if (!validStatuses.includes(newStatus)) {

        message.textContent =
            "Please select a valid status.";

        return;
    }

    // Check if status has changed

    if (selectedQuote.status === newStatus) {

        message.textContent =
            "No changes were made.";

        return;
    }

    // Update the selected quote

    selectedQuote.status = newStatus;

    // Save changes to localStorage

    const saved = saveStatusesToStorage();

    // Update status inside popup

    document.getElementById("detailStatus").textContent =
        newStatus;

    // Refresh dashboard statistics

    updateStatistics();

    // Refresh customer quote table

    displayQuotes();

    // Display confirmation message

    if (saved) {

        message.textContent =
            `Quote status updated to ${newStatus} and saved.`;

    } else {

        message.textContent =
            "Status updated, but browser storage is unavailable.";
    }
}

// =========================================
// Reset Demo Data
// =========================================

function resetDemoData() {

    const confirmed = window.confirm(
        "Reset all demo quote statuses to their original values?"
    );

    if (!confirmed) {
        return;
    }

    // Remove saved statuses

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

    // Reset selected quote

    selectedQuote = null;

    // Reset status filter

    document.getElementById("statusFilter").value =
        "All";

    // Clear search input

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
// Close Quote Details Popup
// =========================================

function closeQuoteDetails() {

    const dialog =
        document.getElementById("quoteDetailsDialog");

    dialog.close();
}

// =========================================
// Initialize Dashboard
// =========================================

function initializeDashboard() {

    // Restore previously saved statuses

    loadSavedStatuses();

    // Display dashboard

    updateStatistics();

    displayQuotes();

    // Status filter

    document.getElementById("statusFilter")
        .addEventListener("change", displayQuotes);

    // Customer search

    document.getElementById("searchInput")
        .addEventListener("input", displayQuotes);

    // Save quote status

    document.getElementById("saveStatus")
        .addEventListener("click", saveQuoteStatus);

    // Close popup using X button

    document.getElementById("closeDetails")
        .addEventListener("click", closeQuoteDetails);

    // Close popup using footer button

    document.getElementById("closeDetailsFooter")
        .addEventListener("click", closeQuoteDetails);

    // Reset selected quote when popup closes

    document.getElementById("quoteDetailsDialog")
        .addEventListener("close", () => {

            selectedQuote = null;
        });

    // Reset Demo button

    const resetButton =
        document.getElementById("resetDemo");

    if (resetButton) {

        resetButton.addEventListener(
            "click",
            resetDemoData
        );
    }
}

// Start dashboard when page loads

document.addEventListener(
    "DOMContentLoaded",
    initializeDashboard
);
