document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('slider-container');
    const overlay = document.getElementById('ela-overlay');
    const handle = document.getElementById('handle');
    var resultSection = document.getElementById('results-section');
    if (resultSection) {
        resultSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    } 

    if (container) {
        const move = (e) => {
            let x = e.type.includes('touch') ? e.touches[0].pageX : e.pageX;
            let rect = container.getBoundingClientRect();
            let pos = ((x - rect.left) / rect.width) * 100;

            pos = Math.max(0, Math.min(pos, 100)); // Clamp between 0-100

            overlay.style.clipPath = `inset(0 0 0 ${pos}%)`;
            handle.style.left = `${pos}%`;
        };
        window.onload = function () {
            container.scrollIntoView({ behavior: 'smooth' });
        };
        // Event Listeners
        const startDragging = () => {
            window.addEventListener('mousemove', move);
            window.addEventListener('touchmove', move);
        };

        const stopDragging = () => {
            window.removeEventListener('mousemove', move);
            window.removeEventListener('touchmove', move);
        };

        handle.addEventListener('mousedown', startDragging);
        handle.addEventListener('touchstart', startDragging);
        window.addEventListener('mouseup', stopDragging);
        window.addEventListener('touchend', stopDragging);
    }
});

document.querySelector('form').onsubmit = function () {
    document.getElementById('analyzeBtn').innerText = "Analyzing Pixels...";
    var spinner = document.getElementById('btnSpinner');
    if (spinner) {
        spinner.classList.remove('d-none');s
    }
    document.getElementById('analyzeBtn').disabled = true;
};

const dropZone = document.getElementById('drop-zone');
const fileInput = document.getElementById('fileInput');

if (dropZone && fileInput) {
    dropZone.onclick = () => fileInput.click();
    fileInput.onchange = (e) => {
        if (e.target.files.length) {
            document.getElementById('fileNameDisplay').innerText = e.target.files[0].name;
            document.getElementById('fileNameDisplay').classList.remove('d-none');
            document.getElementById('previewIcon').classList.replace('text-primary', 'text-success');
        }
    };
}
