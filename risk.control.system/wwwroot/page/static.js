const savedLogo = localStorage.getItem('siteLogo');
if (savedLogo) {
    document.querySelector('.login-logo').src = savedLogo;
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