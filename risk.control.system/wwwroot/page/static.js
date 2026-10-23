const logoPath = localStorage.getItem('logoPath');
if (logoPath) {
    document.querySelector('.login-logo').src = logoPath;
}

const websiteName = localStorage.getItem('websiteName');
if (websiteName) {
    const nameElements = document.querySelectorAll('.websiteName');
    nameElements.forEach(el => {
        // If it's the <title> tag, change the text carefully
        if (el.tagName === 'TITLE') {
            el.innerText = el.innerText.replace("Loading...", websiteName);
        } else {
            // For <span> or <b> tags
            el.innerText = websiteName;
        }
    });
}

const params = new URLSearchParams(window.location.search);
const message = params.get('message');

// Display the message or a default fallback
const messageElement = document.getElementById('message');
if (message) {
    messageElement.textContent = message;
} else {
    messageElement.textContent = "No status message provided.";
}