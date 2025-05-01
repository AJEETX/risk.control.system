document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.map-image').forEach(function (img) {
        img.addEventListener('click', function () {
            const faceId = this.dataset.faceid;
            const locationId = this.dataset.locationid;
            const claimId = this.dataset.caseid;
            const source = this.dataset.source;

            const modalElement = document.getElementById('mapModal');
            const liveMap = document.getElementById('liveMap');
            const faceMapUrl = document.getElementById('agent-map-url');
            var faceMsg = document.getElementById('agent-msg');
            const spinner = document.getElementById('mapLoadingSpinner');

            // Reset all sections
            if (faceMapUrl) {
                faceMapUrl.src = "";
                faceMapUrl.classList.add('hidden');
            }
            if (faceMsg) {
                faceMsg.innerHTML = `<p>Distance: <em> ... </em>, Duration <em>...}</em>.</p>`;
                faceMsg.classList.add('hidden');
            }
            if (liveMap) liveMap.classList.add('hidden');
            if (spinner) spinner.classList.remove('hidden');
            let apiUrl = '';

            if (source === 'agent') {
                apiUrl = `/api/CaseInvestigationDetails/GetAgentDetail?claimid=${claimId}&faceId=${faceId}&locationId=${locationId}`;
            } else if (source === 'document') {
                apiUrl = `/api/CaseInvestigationDetails/GetDocumentDetail?claimid=${claimId}&docId=${faceId}`;
            } else {
                alert("Invalid data source.");
                return;
            }

            fetch(apiUrl)
                .then(res => res.json())
                .then(data => {
                        if (data && data.center && data.dakota && data.frick && data.url && data.distance && data.duration) {
                        const bootstrapModal = new bootstrap.Modal(modalElement);

                        // Show the modal first
                        bootstrapModal.show();
                            setTimeout(() => {
                                console.log("Initializing map after delay...");
                                if (faceMapUrl) {
                                    faceMapUrl.src = data.url;
                                    faceMapUrl.classList.remove('hidden');
                                }
                                if (faceMsg) {
                                    faceMsg.innerHTML = `<p>Distance: <em>${data.distance}</em>, Duration: <em>${data.duration}</em>.</p>`;
                                    faceMsg.classList.remove('hidden');
                                }
                                if (liveMap) liveMap.classList.remove('hidden');
                                if (spinner) spinner.classList.add('hidden');
                            }, 1000);
                    } else {
                        alert("Map location not available.");
                    }
                })
                .catch(error => console.error('Map load error:', error));
        });
    });
});

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.preview-image').forEach(function (img) {
        img.addEventListener('click', function () {
            const src = this.getAttribute('src');
            const modalImage = document.getElementById('modalImage');
            modalImage.setAttribute('src', src);

            const imageModal = new bootstrap.Modal(document.getElementById('imageModal'));
            imageModal.show();
        });
    });
});
