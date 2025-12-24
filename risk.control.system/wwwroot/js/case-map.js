document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.map-image').forEach(function (img) {
        img.addEventListener('click', function () {
            const faceId = this.dataset.faceid;
            const caseId = $('#caseId').val();
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
                apiUrl = `/api/CaseInvestigationDetails/GetAgentDetail?caseId=${caseId}&faceId=${faceId}`;
            } else if (source === 'document') {
                apiUrl = `/api/CaseInvestigationDetails/GetDocumentDetail?caseId=${caseId}&docId=${faceId}`;
            }
            else if (source === 'media') {
                apiUrl = `/api/CaseInvestigationDetails/GetMediaDetail?caseId=${caseId}&docId=${faceId}`;
            }
            else if (source === 'face') {
                apiUrl = `/api/CaseInvestigationDetails/GetFaceDetail?caseId=${caseId}&faceId=${faceId}`;
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
                                    // Clear container
                                    faceMsg.innerHTML = "";

                                    // Create paragraph
                                    const p = document.createElement("p");
                                    p.classList.add("agent-block");

                                    // Build text segments using createTextNode
                                    p.append(
                                        document.createTextNode("Distance from "),
                                        document.createTextNode(data.address || ""), // SAFE
                                        document.createTextNode(" Address: ")
                                    );

                                    // Distance <em>
                                    const dist = document.createElement("em");
                                    dist.textContent = data.distance || "";
                                    p.append(dist);

                                    // Middle text
                                    p.append(document.createTextNode(", Duration: "));

                                    // Duration <em>
                                    const dur = document.createElement("em");
                                    dur.textContent = data.duration || "";
                                    p.append(dur);

                                    p.append(document.createTextNode("."));

                                    // Append safely
                                    faceMsg.appendChild(p);
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
