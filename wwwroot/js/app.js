
const quoteForm = document.getElementById("quoteForm");
const formMessage = document.getElementById("formMessage");

quoteForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    const quoteRequest = {
        customerName: document.getElementById("customerName").value.trim(),
        email: document.getElementById("email").value.trim(),
        phone: document.getElementById("phone").value.trim(),
        address: document.getElementById("address").value.trim(),
        service: document.getElementById("service").value,
        message: document.getElementById("message").value.trim()
    };

    formMessage.textContent = "Submitting your request...";

    try {
        const response = await fetch("/api/quotes", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(quoteRequest)
        });

        if (!response.ok) {
            throw new Error("Unable to submit quote request.");
        }

        formMessage.textContent =
            "Thank you! Your quote request has been submitted.";

        quoteForm.reset();

    } catch (error) {
        console.error("Quote submission error:", error);

        formMessage.textContent =
            "Something went wrong. Please try again.";
    }
});
