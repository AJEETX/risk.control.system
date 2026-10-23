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

document.addEventListener('DOMContentLoaded', function () {
    const closeBtn = document.getElementById('closeWindowBtn');

    if (closeBtn) {
        closeBtn.addEventListener('click', function () {
            window.close();

            // Fallback notification if browser security restricts programmatic window closing
            setTimeout(function () {
                alert("You can now safely close this window or tab.");
            }, 300);
        });
    }
});