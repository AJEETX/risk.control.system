$(document).ready(function () {
    // Load dynamic data first
    $.ajax({
        url: '/Claim/GetClaimSubmissionsJson',
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

                // File type handling (use camelCase 'type')
                if (data.type === 'file') {
                    const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(val);
                    if (isImg) {
                        return `<a href="${val}" target="_blank">
                                    <img src="${val}" class="img-thumbnail table-profile-image" />
                                </a>`;
                    }
                    return `<a href="${val}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2 small text-nowrap">
                                <i class="bi bi-download"></i> View
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
                            <a href="${mapUrl}" target="_blank" class="btn btn-xs btn-outline-danger py-0 px-2 text-nowrap shadow-sm" title="Open in Google Maps">
                                <i class="bi bi-geo-alt-fill text-danger me-1"></i>Map
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
        var url = '/Form/DeleteSubmission/' + id; // Replace with your actual API URL

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
// 7. Child Row Panel Builder (Updated to camelCase)
function formatChildPanel(rowData) {
    let policyHtml = '', nomineeHtml = '', claimHtml = '';
    // Read the key from the DOM wrapper
    const googleMapsApiKey = $('#submissionsTableWrapper').data('google-maps-key') || '';

    if (rowData && rowData.values) { // camelCase 'values'
        Object.values(rowData.values).forEach(item => {
            const val = item.value;
            let displayValue = val || '<em class="text-muted small">Empty</em>';

            // 1. File type handling
            if (item.type === 'file' && val) {
                const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(val);
                displayValue = isImg
                    ? `<a href="${val}" target="_blank"><img src="${val}" class="img-thumbnail" style="max-height: 80px;" /></a>`
                    : `<a href="${val}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2">Download File</a>`;
            }

            // 2. Address type handling (Fixed variable name to displayValue)
            if (item.type === 'address' && val) {
                const encodedAddr = encodeURIComponent(val);
                const mapUrl = `https://www.google.com/maps/search/?api=1&query=${encodedAddr}&key=${googleMapsApiKey}`;

                displayValue = `
                    <div class="d-flex align-items-center gap-2">
                        <span>${val}</span>
                        <a href="${mapUrl}" target="_blank" class="btn btn-xs btn-outline-danger py-0 px-2 text-nowrap shadow-sm" title="Open in Google Maps">
                            <i class="bi bi-geo-alt-fill me-1"></i>Map
                        </a>
                    </div>`;
            }

            const block = `
                <div class="row py-1 border-bottom mx-0">
                    <div class="col-sm-5 fw-semibold text-secondary small">${item.label}</div>
                    <div class="col-sm-7 text-dark text-break">${displayValue}</div>
                </div>`;

            if (item.section === 'Nominee' || item.section === 'LifeAssured') nomineeHtml += block;
            else if (item.section === 'ClaimDetail') claimHtml += block;
            else policyHtml += block;
        });
    }

    // 3. Added 3rd column for Claim Details
    return `
        <div class="p-3 bg-light border rounded shadow-sm m-2">
            <div class="row">
                <div class="col-md-4 border-end">
                    <h6 class="text-primary border-bottom pb-2 fw-bold">Policy Info</h6>
                    ${policyHtml || '<p class="text-muted small">No data</p>'}
                </div>
                <div class="col-md-4 border-end">
                    <h6 class="text-success border-bottom pb-2 fw-bold">Life Assured Details</h6>
                    ${nomineeHtml || '<p class="text-muted small">No data</p>'}
                </div>
                <div class="col-md-4">
                    <h6 class="text-warning border-bottom pb-2 fw-bold">Claim Details</h6>
                    ${claimHtml || '<p class="text-muted small">No data</p>'}
                </div>
            </div>
        </div>`;
}