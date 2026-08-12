$(document).ready(function () {
    // Load dynamic data first
    $.ajax({
        url: '/api/Investigation/GetActiveClaims',
        type: 'POST',
        contentType: 'application/json; charset=utf-8', // 👈 Fix 1: Explicitly specify JSON content type
        dataType: 'json',
        data: JSON.stringify({
            draw: 1,
            start: 0,
            length: 10,
            search: '',
            orderColumn: 0,
            orderDir: 'asc'
        }),
        success: function (response) {
            // 👈 Fix 2: Use response.formFields matching C# backend key
            if (response && response.formFields) {
                initDynamicDataTable(response.formFields, response.data);
            } else {
                console.error("Invalid response format:", response);
            }
        },
        error: function (xhr, status, error) {
            console.error("AJAX Error Details:", xhr.responseText);
            alert('Failed to load submissions data.');
        }
    });
});

function initDynamicDataTable(dynamicFields, submissionsData) {
    // 1. Fixed leading column (Expand button)
    const columnsConfig = [];
    const defaultVisibleFields = dynamicFields.slice(0, 6);
    // 2. Add Dynamic Columns based on FormFields
    defaultVisibleFields.forEach(field => {
        columnsConfig.push({
            // Use camelCase 'values'
            data: `values.${field.id}`,
            title: field.label,
            className: 'align-middle text-center',
            defaultContent: '<span class="text-muted small">-</span>',
            render: function (data, type, row) {
                // Use camelCase 'value'
                if (!data || !data.value) {
                    return '<span class="text-muted small">-</span>';
                }

                const val = data.value;

                // File type handling (use camelCase 'type')
                if (data.type === 'file') {
                    const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(val);
                    if (isImg) {
                        return `<a href="${val}" target="_blank">
                                    <img src="${val}" class="img-thumbnail table-profile-image" />
                                </a>`;
                    }
                    return `<a href="${val}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2 small text-nowrap">
                                <i class="fa fa-download"></i> View
                            </a>`;
                }
                if (data.type === 'address') {
                    const encodedAddr = encodeURIComponent(val);
                    const googleMapsApiKey = $('#submissionsTableWrapper').data('google-maps-key') || '';

                    // Construct URL with API Key if needed
                    const mapUrl = `https://www.google.com/maps/search/?api=1&query=${encodedAddr}&key=${googleMapsApiKey}`;
                    const truncatedVal = val.length > 30 ? val.substring(0, 30) + '...' : val;
                    return `
                        <div class="d-flex align-items-center justify-content-between gap-2 text-start px-1">
                            <span class="text-truncate">${truncatedVal}</span>
                            <a href="${mapUrl}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2 text-nowrap shadow-sm" title="Open in Google Maps">
                                <i class="fa fa-geo text-primary me-1"></i>Map
                            </a>
                        </div>`;
                }

                // 3. Fallback for text/number/date
                return `<span class="text-truncate d-inline-block mw-100">${val}</span>`;
            }
        });
    });

    // 3. Fixed trailing columns (Submitted At & Actions)
    columnsConfig.push(
        {
            data: 'status',
            title: 'Status',
            orderable: false,
            className: 'align-middle text-nowrap',
            defaultContent: '<span class="text-muted small">-</span>',
            render: function (data) {
                return data || '<span class="text-muted small">-</span>';
            }
        },
        {
        data: 'vendorEmail',
        title: 'Agency',
        orderable: false,
        className: 'align-middle text-nowrap',
        defaultContent: '<span class="text-muted small">-</span>',
        render: function (data) {
            return data
                ? `<span class="small bold blue"> ${data}</span>`
                : '<span class="text-muted small">-</span>';
        }
    },
        {
            data: 'submittedAt', // camelCase
            title: 'Submitted',
            className: 'align-middle text-nowrap',
            render: function (data) {
                if (!data) return '-';
                const dateObj = new Date(data);
                if (isNaN(dateObj.getTime())) return data;
                const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
                return `<small><strong> ${String(dateObj.getDate()).padStart(2, '0')}-${months[dateObj.getMonth()]}-${dateObj.getFullYear()} ${String(dateObj.getHours()).padStart(2, '0')}:${String(dateObj.getMinutes()).padStart(2, '0')} </string></small>`;
            }
        },
        {
            title: 'Expand', // Empty header so text doesn't force column width
            className: 'align-middle text-center expand-btn-cell',
            data: null,
            render: function () {
                return '<i class="fa fa-plus-circle text-success toggle-icon"  title="Expand Details"></i>';
            }
        }
    );

    // 4. Initialize DataTables
    const table = $('#dataTable').DataTable({
        data: submissionsData,
        columns: columnsConfig,
        autoWidth: false,
        order: [[columnsConfig.length - 3, 'desc']], // Sort by Date Submitted
        columnDefs: [
            {
                targets: 'expand-btn-cell', // Target the class name
                width: '10px',
                orderable: false,
                searchable: false
            },
            // Disable sorting on the first column (index 0) and last column (-1)
            { orderable: false, targets: [0, 2, 3, 4, 6, -1] }
        ],
        autoWidth: false,
        drawCallback: function () {
            // Targets every <td> in the table body on every render
            $('#dataTable tbody td').attr('data-bs-toggle', 'tooltip');
            // Set title attribute to the text contents of every cell
            $('#dataTable tbody td').each(function () {
                const cellText = $(this).text().trim();
                if (cellText && cellText !== '-') {
                    $(this).attr('title', cellText);
                }
            });

            // Initialize Bootstrap tooltips if Bootstrap is loaded
            if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
                const tooltipTriggerList = [].slice.call(document.querySelectorAll('#dataTable tbody td[title]'));
                tooltipTriggerList.map(function (tooltipTriggerEl) {
                    return new bootstrap.Tooltip(tooltipTriggerEl);
                });
            }
        }
    });

    // 5. Expand row listener
    $('#dataTable tbody').on('click', 'td.expand-btn-cell', function () {
        const tr = $(this).closest('tr');
        const row = table.row(tr);
        const icon = $(this).find('.toggle-icon');

        if (row.child.isShown()) {
            row.child.hide();
            tr.removeClass('parent-expanded');
            icon.removeClass('fa-minus-circle text-danger').addClass('fa-plus-circle text-success');
        } else {
            row.child(formatChildPanel(row.data())).show();
            tr.addClass('parent-expanded');
            icon.removeClass('fa-plus-circle text-success').addClass('fa-minus-circle text-danger');
            $.get(`/Claim/GetCaseTimelinesPartial?claimId=${row.data().id}`, function (htmlResponse) {
                tr.next('tr').find('.timeline-container-placeholder').html(htmlResponse);
            });
        }
    });
    // Listener for Withdraw button inside child panel with Reason Input
    $('#dataTable tbody').on('click', '.withdraw-claim-btn', function (e) {
        e.stopPropagation();
        const claimId = $(this).data('id');
        const policyNumber = $(this).data('policy-number') || 'N/A';

        // Retrieve the anti-forgery token from the page
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.confirm({
            title: 'Withdraw Claim',
            content: `
            <div class="form-group">
                <p class="mb-2">Are you sure you want to withdraw claim for Policy Number: <strong>${policyNumber}</strong>?</p>
                <label class="fw-bold form-label text-secondary small">Reason for Withdrawal <span class="text-danger">*</span></label>
                <textarea id="withdrawReasonInput" class="form-control" rows="3" placeholder="Please provide a clear reason for withdrawal..."></textarea>
                <div id="withdrawReasonError" class="text-danger small mt-1 d-none">Please enter a reason before proceeding.</div>
            </div>`,
            type: 'red',
            typeAnimated: true,
            icon: 'fa fa-exclamation-triangle',
            buttons: {
                confirm: {
                    text: 'Confirm Withdrawal',
                    btnClass: 'btn-danger',
                    action: function () {
                        const self = this;
                        const reason = self.$content.find('#withdrawReasonInput').val().trim();
                        const $errorDiv = self.$content.find('#withdrawReasonError');

                        if (!reason) {
                            $errorDiv.removeClass('d-none');
                            self.$content.find('#withdrawReasonInput').addClass('is-invalid').focus();
                            return false;
                        }

                        $errorDiv.addClass('d-none');
                        self.$content.find('#withdrawReasonInput').removeClass('is-invalid');

                        self.showLoading();

                        $.ajax({
                            url: `/WithdrawCase/WithdrawClaim/${claimId}`,
                            type: 'POST',
                            contentType: 'application/json; charset=utf-8',
                            headers: {
                                "RequestVerificationToken": token
                            },
                            data: JSON.stringify({
                                claimId: claimId,
                                reason: reason
                            })
                        }).done(function (res) {
                            self.close();

                            // Single success alert with OK button handling
                            $.alert({
                                title: 'Withdrawn Successfully',
                                content: `Claim for Policy <strong>${policyNumber}</strong> has been withdrawn.`,
                                type: 'green',
                                icon: 'fa fa-check-circle',
                                buttons: {
                                    ok: {
                                        text: 'OK',
                                        btnClass: 'btn-green',
                                        action: function () {
                                            location.reload();
                                        }
                                    }
                                }
                            });
                        }).fail(function (xhr) {
                            self.hideLoading();
                            console.error("Withdrawal error:", xhr.responseText);

                            const errorMsg = xhr.responseJSON && xhr.responseJSON.message
                                ? xhr.responseJSON.message
                                : 'Failed to withdraw claim. Please try again.';

                            $.alert({
                                title: 'Withdrawal Failed',
                                content: errorMsg,
                                type: 'red',
                                icon: 'fa fa-times-circle'
                            });
                        });

                        return false;
                    }
                },
                cancel: {
                    text: 'Cancel',
                    btnClass: 'btn-default'
                }
            },
            onContentReady: function () {
                this.$content.find('#withdrawReasonInput').focus();
            }
        });
    });
}
// Updated Child Row Panel Builder without inline styles
function formatChildPanel(rowData) {
    let policyNumber = 'N/A';
    const googleMapsApiKey = $('#submissionsTableWrapper').data('google-maps-key') || '';

    // Data buckets for each card section
    const sections = {
        policy: { fields: [], files: [] },
        nominee: { fields: [], files: [], image: null },
        claim: { fields: [], files: [] }
    };

    if (rowData && rowData.values) {
        Object.values(rowData.values).forEach(item => {
            const val = item.value;
            if (item.type && item.type.includes('policyNumber')) {
                policyNumber = val || 'N/A';
            }

            // Determine target section
            let secKey = 'policy';
            if (item.section === 'Nominee' || item.section === 'LifeAssured') secKey = 'nominee';
            else if (item.section === 'ClaimDetail') secKey = 'claim';

            // Process content types
            if (item.type === 'file' && val) {
                const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(val);
                if (isImg && secKey === 'nominee') {
                    sections.nominee.image = val;
                } else {
                    sections[secKey].files.push({ label: item.label, url: val });
                }
            } else if (item.type === 'address' && val) {
                const encodedAddr = encodeURIComponent(val);
                const mapUrl = `https://www.google.com/maps/search/?api=1&query=${encodedAddr}&key=${googleMapsApiKey}`;
                sections[secKey].fields.push({ label: item.label, value: val, isAddress: true, mapUrl: mapUrl });
            } else {
                sections[secKey].fields.push({ label: item.label, value: val || '-' });
            }
        });
    }

    // Helper: Render key-value text list
    function renderFieldsList(fields) {
        if (!fields || fields.length === 0) return '<p class="text-muted small">No data</p>';
        return fields.map(f => {
            if (f.isAddress) {
                return `
                    <div class="mb-2">
                        <div class="fw-bold text-secondary small">${f.label}</div>
                        <div class="small text-dark text-break">${f.value}</div>
                        ${f.mapUrl ? `<a href="${f.mapUrl}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2 text-nowrap shadow-sm" title="Open in Google Maps">
                            <i class="fa fa-geo me-1"></i>Map
                        </a>` : ''}
                    </div>`;
            }
            return `
                <div class="mb-2">
                    <div class="fw-bold text-secondary small">${f.label}</div>
                    <div class="small text-dark text-break">${f.value}</div>
                </div>`;
        }).join('');
    }

    // Attachment Box 1: Policy Document
    const policyFile = sections.policy.files[0];
    const policyDocHtml = policyFile ? `
        <div class="border rounded p-3 text-center bg-light d-flex flex-column align-items-center justify-content-center">
            <i class="fa fa-file-earmark-text display-5 text-secondary mb-2"></i>
            <a href="${policyFile.url}" target="_blank" class="btn btn-xs btn-outline-primary text-nowrap py-1 px-2 small">
                <i class="fa fa-paperclip me-1"></i> ${policyFile.label}
            </a>
        </div>` : `
        <div class="border rounded p-3 text-center bg-light d-flex flex-column align-items-center justify-content-center">
            <i class="fa fa-file-earmark-text display-5 text-muted mb-2"></i>
            <span class="text-muted small">No Document</span>
        </div>`;

    // Attachment Box 2: Nominee Photo
    const nomineeImgHtml = sections.nominee.image ? `
        <img src="${sections.nominee.image}" class="img-thumbnail rounded shadow-sm nominee-photo" alt="Nominee Photo" />
    ` : `
        <div class="border rounded p-3 text-center bg-light d-flex flex-column align-items-center justify-content-center nominee-photo-placeholder">
            <i class="fa fa-person display-4 text-muted"></i>
            <span class="text-muted small mt-1">No Image</span>
        </div>`;

    // Attachment Box 3: Claim Folder / Document
    const claimFile = sections.claim.files[0];
    const claimDocHtml = claimFile ? `
        <div class="border rounded p-3 text-center bg-light d-flex flex-column align-items-center justify-content-center">
            <i class="fa fa-folder2-open display-5 text-secondary mb-2"></i>
            <a href="${claimFile.url}" target="_blank" class="btn btn-xs btn-outline-primary text-nowrap py-1 px-2 small">
                <i class="fa fa-paperclip me-1"></i> ${claimFile.label}
            </a>
        </div>` : `
        <div class="border rounded p-3 text-center bg-light d-flex flex-column align-items-center justify-content-center">
            <i class="fa fa-folder2-open display-5 text-muted mb-2"></i>
            <span class="text-muted small">No Document</span>
        </div>`;

    return `
        <div class="p-3 bg-light border rounded shadow-sm m-2">
            <div class="row g-3">
                <!-- 1. Policy Details Card -->
                <div class="col-lg-4">
                    <div class="card h-100 border shadow-sm rounded-2 overflow-hidden">
                        <div class="card-header bg-white border-bottom py-2 d-flex align-items-center">
                            <i class="far fa-file-alt text-primary"></i>
                                <span>&nbsp;&nbsp;</span>
                            <h6 class="mb-0 fw-semibold text-primary">Policy Details</h6>
                        </div>
                        <div class="bg-success text-white px-2 py-1 d-flex justify-content-between align-items-center card-ribbon-bar">
                            <i class="fa fa-bookmark-fill"></i>
                            <i class="fa fa-file-earmark-pdf-fill"></i>
                        </div>
                        <div class="card-body p-3">
                            <div class="row h-100">
                                <div class="col-7 pe-1">
                                    ${renderFieldsList(sections.policy.fields)}
                                </div>
                                <div class="col-5 ps-1 d-flex align-items-start justify-content-center">
                                    ${policyDocHtml}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- 2. Nominee Details Card -->
                <div class="col-lg-4">
                    <div class="card h-100 border shadow-sm rounded-2 overflow-hidden">
                        <div class="card-header bg-white border-bottom py-2 d-flex align-items-center">
                            <i class="fas fa-user-plus me-2 text-success"></i>
                                <span>&nbsp;&nbsp;</span>
                            <h6 class="mb-0 fw-semibold text-success">Nominee Details</h6>
                        </div>
                        <div class="bg-success text-white px-2 py-1 d-flex justify-content-between align-items-center card-ribbon-bar">
                            <i class="fa fa-bookmark-fill"></i>
                            <i class="fa fa-person-fill"></i>
                        </div>
                        <div class="card-body p-3">
                            <div class="row h-100">
                                <div class="col-7 pe-1">
                                    ${renderFieldsList(sections.nominee.fields)}
                                </div>
                                <div class="col-5 ps-1 d-flex align-items-start justify-content-center">
                                    ${nomineeImgHtml}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- 3. Claim Details Card -->
                <div class="col-lg-4">
                    <div class="card h-100 border shadow-sm rounded-2 overflow-hidden">
                        <div class="card-header bg-white border-bottom py-2 d-flex align-items-center">
                            <i class="fas fa-clipboard-list me-2 text-warning"></i>
                                <span>&nbsp;&nbsp;</span>
                            <h6 class="mb-0 fw-semibold text-warning">Claim Details</h6>
                        </div>
                        <div class="bg-success text-white px-2 py-1 d-flex justify-content-between align-items-center card-ribbon-bar">
                            <i class="fa fa-bookmark-fill"></i>
                            <i class="fa fa-file-earmark-fill"></i>
                        </div>
                        <div class="card-body p-3">
                            <div class="row h-100">
                                <div class="col-7 pe-1">
                                    ${renderFieldsList(sections.claim.fields)}
                                </div>
                                <div class="col-5 ps-1 d-flex align-items-start justify-content-center">
                                    ${claimDocHtml}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Timeline Container -->
            <div class="mt-3">
                <div class="timeline-container-placeholder">
                    <div class="text-center py-3 text-muted small">
                        <i class="fas fa-sync fa-spin me-1"></i> Loading Timeline...
                    </div>
                </div>
            </div>

            <!-- Footer Actions -->
            <div class="d-flex justify-content-end mt-3 pt-2 border-top">
                <button type="button" class="btn btn-sm btn-outline-danger withdraw-claim-btn" data-id="${rowData.id}" data-policy-number="${policyNumber}">
                    <i class="fa fa-arrow-return-left me-1"></i> Withdraw
                </button>
            </div>
        </div>`;
}
