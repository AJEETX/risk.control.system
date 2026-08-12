$(document).ready(function () {
    // Load dynamic data first
    $.ajax({
        url: '/Claim/Get',
        type: 'GET',
        success: function (response) {
            initDynamicDataTable(response.fields, response.data);
        },
        error: function () {
            alert('Failed to load submissions data.');
        }
    });
});
function updateFooterButtonStates() {
    const selectedCount = $('#dataTable tbody .select-row-checkbox:checked').length;
    const hasSelection = selectedCount > 0;

    // Enable buttons if at least one checkbox is checked, otherwise disable
    $('#allocatedcase, #deletecase').prop('disabled', !hasSelection);
}
function initDynamicDataTable(dynamicFields, submissionsData) {
    // 1. Fixed leading column (Expand button)
    const columnsConfig = [{
        data: null,
        title: '<input type="checkbox" id="selectAllCheckboxes" />',
        "sDefaultContent": "<i class='far fa-edit' data-bs-toggle='tooltip' title='Incomplete'></i>",
        "bSortable": false,
        "mRender": function (data, type, row) {
            return `<input type="checkbox" class="select-row-checkbox" value="${row.id}">`;
        }
    }];
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
                const status = row.status || row.Status;
                const statusTitle = row.withdrawlComments;

                if (data.type && data.type =='policyNumber') {
                    const titleAttr = statusTitle ? `title="${statusTitle}"` : '';
                    if (row.withDrawable || statusTitle) {
                        return `<span ${titleAttr} class="text-truncate d-inline-block mw-100 fw-bold">${val} <span class="text-danger">*</span></span>`;
                    }
                }
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
                                <i class="fa fa-geo-alt-fill text-primary me-1"></i>Map
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
            title: '', // Empty header so text doesn't force column width
            className: 'align-middle text-center expand-btn-cell',
            data: null,
            render: function () {
                return '<i class="fa fa-plus-circle text-success toggle-icon"  title="Expand Details"></i>';
            }
        },
        {
            data: 'id', // camelCase
            title: 'Actions',
            orderable: false,
            searchable: false,
            className: 'align-middle text-center text-nowrap',
            render: function (data) {
                return `
                <a data-id="${data}" class="btn btn-xs btn-info refresh-btn"><i class="fas fa-external-link-alt"></i> Assign</a>&nbsp;
                    <a data-id="${data}"  class="btn btn-xs btn-warning"><i class="fas fa-edit"></i> Edit</a>
                    <button type="button" class="btn btn-xs btn-danger btn-delete" data-id="${data}"><i class="fa fa-trash"></i> Delete</button>
                `;
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
            updateFooterButtonStates();

            // Uncheck 'Select All' if page/draw changes
            $('#selectAllCheckboxes').prop('checked', false);

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
        }
    });
    $('#dataTable tbody').on('click', '.btn-delete', function (e) {
        e.preventDefault();
        var $btn = $(this);
        var $spinner = $(".submit-progress"); // global spinner (you already have this)

        const id = $(this).data('id');
        var url = '/Claim/Delete/' + id; // Replace with your actual API URL

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete this case?',
            type: 'red',
            icon: 'fas fa-trash',
            buttons: {
                confirm: {
                    text: 'Yes, delete it',
                    btnClass: 'btn-red',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');

                        $.ajax({
                            url: url,
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                id: id
                            },
                            success: function (response) {
                                // Show success message
                                $.alert({
                                    title: 'Deleted!',
                                    content: response.message || 'Case deleted successfully.',
                                    closeIcon: true,
                                    type: 'red',
                                    icon: 'fas fa-trash',
                                    buttons: {
                                        ok: {
                                            text: 'OK',
                                            btnClass: 'btn-default',
                                        }
                                    }
                                });

                                location.reload(); // false = don't reset paging
                            },
                            error: function (xhr, status, error) {
                                console.error("Delete failed:", xhr.responseText);
                                $.alert({
                                    title: 'Error!',
                                    content: 'Failed to delete the case.',
                                    type: 'red'
                                });
                                if (xhr.status === 401 || xhr.status === 403) {
                                    window.location.href = '/Account/Login';
                                } else {
                                    $.alert({
                                        title: 'Error!',
                                        content: 'Unexpected error occurred.',
                                        type: 'red'
                                    });
                                }
                            },
                            complete: function () {
                                $spinner.addClass("hidden");
                                // ✅ Re-enable button and restore text
                                $btn.prop("disabled", false).html('<i class="fas fa-trash"></i> Delete');
                            }
                        });
                    }
                },
                cancel: function () {
                    // Do nothing
                }
            }
        });
    });
    // 1. "Select All" header checkbox handler
    $('#dataTable').on('click', '#selectAllCheckboxes', function () {
        const isChecked = $(this).is(':checked');
        $('#dataTable tbody .select-row-checkbox').prop('checked', isChecked);
        updateFooterButtonStates();
    });

    // 2. Individual row checkbox handler
    $('#dataTable tbody').on('change', '.select-row-checkbox', function () {
        const totalCheckboxes = $('#dataTable tbody .select-row-checkbox').length;
        const checkedCheckboxes = $('#dataTable tbody .select-row-checkbox:checked').length;

        // Keep header checkbox in sync
        $('#selectAllCheckboxes').prop('checked', totalCheckboxes > 0 && totalCheckboxes === checkedCheckboxes);

        // Update button states
        updateFooterButtonStates();
    });
}
function showSpinnerOnButton(selector, spinnerText) {
    $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
}
$('body').on('click', 'a.btn-warning', function (e) {
    e.preventDefault();
    const id = $(this).data('id');
    showedit(id, this);
});
$('body').on('click', 'a.btn-info', function (e) {
    e.preventDefault();
    const id = $(this).data('id');
    showdetail(id, this);
});

function showedit(id, element) {
    id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
    $("body").addClass("submit-progress-bg");
    setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

    showSpinnerOnButton(element, "Edit");

    const editUrl = `/Claim/Edit/${encodeURIComponent(id)}`;

    setTimeout(() => {
        window.location.href = editUrl;
    }, 1000);
}
function showdetail(id, element) {
    id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
    $("body").addClass("submit-progress-bg");
    setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

    showSpinnerOnButton(element, "Assign");

    const editUrl = `/Claim/EmpanelledAgencies/${encodeURIComponent(id)}`;

    setTimeout(() => {
        window.location.href = editUrl;
    }, 1000);
}
// Updated Child Row Panel Builder without inline styles
function formatChildPanel(rowData) {
    let policyNumber = 'N/A';
    const googleMapsApiKey = $('#submissionsTableWrapper').data('google-maps-key') || '';
    const isWithdrawable = rowData?.withDrawable ?? rowData?.WithDrawable ?? false;
    const statusTitle = rowData.withdrawlComments;

    const sections = {
        policy: { fields: [], files: [] },
        nominee: { fields: [], files: [], image: null },
        claim: { fields: [], files: [] }
    };

    if (rowData && rowData.values) {
        Object.values(rowData.values).forEach(item => {
            let val = item.value;

            // Handle Policy Number field specifically
            if (item.type && item.type=='policyNumber') {
                if (val) {
                    const isSpecialStatus = isWithdrawable;
                    const titleAttr = statusTitle ? `title="${statusTitle}"` : '';

                    // Wrap in span with title attribute if special status applies
                    val = isSpecialStatus
                        ? `<span ${titleAttr} class="fw-bold">${val} <span class="text-danger">*</span></span>`
                        : val;
                } else {
                    val = 'N/A';
                }
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
        <div class="border rounded p-3 text-center bg-light d-flex flex-column align-items-center justify-content-center  doc-preview-box">
            <i class="fa fa-file-earmark-text display-5 text-secondary mb-2"></i>
            <a href="${policyFile.url}" target="_blank" class="btn btn-xs btn-outline-primary text-nowrap py-1 px-2 small">
                <i class="fa fa-paperclip me-1"></i> ${policyFile.label}
            </a>
        </div>` : `
        <div class="border rounded p-3 text-center bg-light d-flex flex-column align-items-center justify-content-center  doc-preview-box">
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
        <div class="border rounded p-3 text-center bg-light d-flex flex-column align-items-center justify-content-center  doc-preview-box">
            <i class="fa fa-folder2-open display-5 text-secondary mb-2"></i>
            <a href="${claimFile.url}" target="_blank" class="btn btn-xs btn-outline-primary text-nowrap py-1 px-2 small">
                <i class="fa fa-paperclip me-1"></i> ${claimFile.label}
            </a>
        </div>` : `
        <div class="border rounded p-3 text-center bg-light d-flex flex-column align-items-center justify-content-center  doc-preview-box">
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
        </div>`;
}

function getStatusTitle(status) {
    if (!status) return '';
    const statusStr = String(status).toLowerCase();

    if (statusStr.includes('withdraw')) {
        return 'Withdrawn';
    }
    if (statusStr.includes('decline') || statusStr.includes('reject')) {
        return 'Decline';
    }
    return '';
}