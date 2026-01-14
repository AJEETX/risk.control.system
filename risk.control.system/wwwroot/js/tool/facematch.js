document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById('faceMatchForm');
    const btnSubmit = document.getElementById('btnSubmit');
    const resultContainer = document.getElementById('resultContainer');
    const scoreBar = document.getElementById('scoreBar');
    const matchPercentage = document.getElementById('matchPercentage');
    const resultStatus = document.getElementById('resultStatus');

    // Handle Drop Zone Previews
    setupDropZone('drop-zone-original', 'OriginalFaceImage', 'previewOriginal');
    setupDropZone('drop-zone-match', 'MatchFaceImage', 'previewMatch');

    function setupDropZone(zoneId, inputId, imgId) {
        const zone = document.getElementById(zoneId);
        const input = document.getElementById(inputId);
        const img = document.getElementById(imgId);

        zone.addEventListener('click', () => input.click());
        input.addEventListener('change', () => {
            const file = input.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = (e) => img.src = e.target.result;
                reader.readAsDataURL(file);
            }
        });
    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        // UI Reset
        btnSubmit.disabled = true;
        document.getElementById('btnText').innerText = "Analyzing ...";
        document.getElementById('btnSpinner').classList.remove('d-none');
        resultContainer.classList.add('d-none');

        const formData = new FormData(this);

        try {
            // Updated endpoint to match your controller action
            const response = await fetch('/FaceMatch/Compare', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                const data = await response.json();
                // Update the UI with the remaining count
                if (data.remaining !== undefined) {
                    console.log(`Tries left: ${data.remaining}`);
                    // Optional: If 0, disable the button so they can't click again
                    if (data.remaining <= 0) {
                        $("#btnSubmit").prop("disabled", true);
                        $("#btnText").html('<i class="fas fa-lock"></i> Daily Limit Reached');
                        $("#usageBadge").removeClass("badge-light").addClass("badge-danger");
                    }
                }
                // data.match is your bool, data.similarity is your float
                showResult(data.similarity, data.match, data.remaining);
            } else {
                const error = await response.json();
                alert("Error: " + (error.message || "Could not process images."));
            }
        } catch (error) {
            console.error("Connection error:", error);
            alert("An error occurred while connecting to the server.");
        } finally {
            btnSubmit.disabled = false;
            document.getElementById('btnText').innerHTML = `<i class="fas fa-barcode"></i> Run Face Match Analysis`;
            document.getElementById('btnSpinner').classList.add('d-none');
        }
    });
    function showResult(score, isMatch, remaining) {
        resultContainer.classList.remove('d-none');

        const targetScore = Math.round(score);
        let currentScore = 0;
        document.getElementById('remainingCount').innerText = remaining;
        const interval = setInterval(() => {
            if (currentScore >= targetScore) {
                clearInterval(interval);

                // 1. Set the final precise decimal value
                matchPercentage.innerText = score.toFixed(1) + "%";

                // 2. STOP THE ANIMATION: Remove the 'animated' and 'striped' classes
                scoreBar.classList.remove("progress-bar-animated", "progress-bar-striped");

                // 3. Optional: Add a 'check' icon to indicate completion
                resultStatus.innerHTML += ' <i class="fas fa-check-double text-small"></i>';
            } else {
                currentScore++;
                matchPercentage.innerText = currentScore + "%";
                scoreBar.style.width = currentScore + "%";
            }
        }, 10);

        // Initial state: Set color and keep animation while "loading"
        if (isMatch) {
            resultStatus.innerHTML = '<span class="badge badge-success px-4 py-2 shadow-sm">IDENTITY VERIFIED</span>';
            scoreBar.className = "progress-bar bg-success progress-bar-striped progress-bar-animated";
        } else {
            resultStatus.innerHTML = '<span class="badge badge-danger px-4 py-2 shadow-sm">IDENTITY MISMATCH</span>';
            scoreBar.className = "progress-bar bg-danger progress-bar-striped progress-bar-animated";
        }
    }
});