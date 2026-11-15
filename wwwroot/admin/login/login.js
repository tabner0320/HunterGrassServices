
"use strict";

// =========================================
// DOM ELEMENTS
// =========================================

const loginForm = document.getElementById("loginForm");
const emailInput = document.getElementById("email");
const passwordInput = document.getElementById("password");
const showPasswordCheckbox =
    document.getElementById("showPassword");

const loginButton = document.getElementById("loginButton");
const loginMessage = document.getElementById("loginMessage");

// =========================================
// SHOW / HIDE PASSWORD
// =========================================

function togglePasswordVisibility() {
    passwordInput.type = showPasswordCheckbox.checked
        ? "text"
        : "password";
}

// =========================================
// DISPLAY LOGIN MESSAGE
// =========================================

function displayMessage(message, type = "") {
    loginMessage.textContent = message;
    loginMessage.className = "login-message";

    if (type) {
        loginMessage.classList.add(type);
    }
}

// =========================================
// GET ANTIFORGERY TOKEN
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
// HANDLE ADMINISTRATOR LOGIN
// =========================================

async function handleLogin(event) {
    event.preventDefault();

    // Prevent duplicate login submissions
    loginButton.disabled = true;
    loginButton.textContent = "Signing In...";

    displayMessage("Signing in...");

    try {
        // Get a fresh antiforgery token
        const token = await getAntiforgeryToken();

        // Prepare login credentials
        const loginRequest = {
            email: emailInput.value.trim(),
            password: passwordInput.value
        };

        // Send credentials to the secure login API
        const response = await fetch(
            "/api/admin/login",
            {
                method: "POST",

                headers: {
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": token
                },

                credentials: "same-origin",

                body: JSON.stringify(loginRequest)
            }
        );

        // Handle successful login
        if (response.ok) {
            displayMessage(
                "Login successful! Redirecting...",
                "success"
            );

            window.location.href = "/admin/";
            return;
        }

        // Handle invalid credentials
        if (response.status === 401) {
            displayMessage(
                "Invalid email or password.",
                "error"
            );
            return;
        }

        // Handle rate limiting
        if (response.status === 429) {
            displayMessage(
                "Too many login attempts. Please wait one minute and try again.",
                "error"
            );
            return;
        }

        // Handle validation or security errors
        if (response.status === 400) {
            const data = await response.json();

            const message = Array.isArray(data)
                ? data.join(" ")
                : data.message ||
                  "Please check your login information.";

            displayMessage(message, "error");
            return;
        }

        // Handle unexpected server responses
        displayMessage(
            "Unable to sign in. Please try again.",
            "error"
        );

    } catch (error) {
        console.error("Login error:", error);

        displayMessage(
            "Unable to connect to the server. Please try again.",
            "error"
        );

    } finally {
        // Restore the login button
        loginButton.disabled = false;
        loginButton.textContent = "Sign In";
    }
}

// =========================================
// INITIALIZE LOGIN PAGE
// =========================================

function initializeLoginPage() {
    showPasswordCheckbox.addEventListener(
        "change",
        togglePasswordVisibility
    );

    loginForm.addEventListener(
        "submit",
        handleLogin
    );

    // Enable administrator login
    loginButton.disabled = false;

    displayMessage(
        "Enter your administrator credentials."
    );
}

// Initialize when the page is ready
document.addEventListener(
    "DOMContentLoaded",
    initializeLoginPage
);
