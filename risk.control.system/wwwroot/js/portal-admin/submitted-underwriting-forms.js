$(document).ready(function () {
    // Load dynamic data first
    $.ajax({
        url: '/Form/GetUnderwritingSubmissionsJson',
        type: 'GET',
        success: function (response) {
            initDynamicDataTable(response.fields, response.data);
        },
        error: function () {
            alert('Failed to load submissions data.');
        }
    });
});
function initDynamicDataTable(dynamicFields, submissionsData) {
    // 1. Fixed leading column (Expand button)
    const columnsConfig = [
        {
            className: 'dt-control align-middle text-center',
            orderable: false,
            data: null,
            defaultContent: '',
            width: '40px'
        }
    ];

    // 2. Add Dynamic Columns based on FormFields
    dynamicFields.forEach(field => {
        columnsConfig.push({
            // Use camelCase 'values'
            data: `values.${field.id}`,
            title: field.label,
            className: 'align-middle',
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
                                    <img src="${val}" class="img-thumbnail" style="max-height: 40px;" />
                                </a>`;
                    }
                    return `<a href="${val}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2 small text-nowrap">
                                <i class="bi bi-download"></i> View
                            </a>`;
                }

                return val;
            }
        });
    });

    // 3. Fixed trailing columns (Submitted At & Actions)
    columnsConfig.push(
        {
            data: 'submittedAt', // camelCase
            title: 'Date Submitted',
            className: 'align-middle text-nowrap',
            width: '160px',
            render: function (data) {
                if (!data) return '-';
                const dateObj = new Date(data);
                if (isNaN(dateObj.getTime())) return data;
                const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
                return `${String(dateObj.getDate()).padStart(2, '0')}-${months[dateObj.getMonth()]}-${dateObj.getFullYear()} ${String(dateObj.getHours()).padStart(2, '0')}:${String(dateObj.getMinutes()).padStart(2, '0')}`;
            }
        },
        {
            data: 'id', // camelCase
            title: 'Actions',
            orderable: false,
            searchable: false,
            className: 'align-middle text-center text-nowrap',
            width: '130px',
            render: function (data) {
                return `
                    <a href="/Form/EditForm/${data}" class="btn btn-sm btn-outline-primary px-2 py-1 me-1"><i class="bi bi-pencil-square"></i> Edit</a>
                    <button type="button" class="btn btn-sm btn-outline-danger px-2 py-1 btn-delete" data-id="${data}"><i class="bi bi-trash"></i> Delete</button>
                `;
            }
        }
    );

    // 4. Initialize DataTables
    const table = $('#submissionsTable').DataTable({
        data: submissionsData,
        columns: columnsConfig,
        order: [[columnsConfig.length - 2, 'desc']], // Sort by Date Submitted
        autoWidth: false
    });

    // 5. Expand row listener
    $('#submissionsTable tbody').on('click', 'td.dt-control', function () {
        const tr = $(this).closest('tr');
        const row = table.row(tr);

        if (row.child.isShown()) {
            row.child.hide();
            tr.removeClass('parent-expanded');
        } else {
            row.child(formatChildPanel(row.data())).show();
            tr.addClass('parent-expanded');
        }
    });

    // 6. Delete event handler
    $('#submissionsTable tbody').on('click', '.btn-delete', function () {
        const id = $(this).data('id');
        if (confirm("Are you sure you want to permanently delete this submission record?")) {
            $.ajax({
                url: `/Form/DeleteSubmission/${id}`,
                type: 'POST',
                success: function (result) {
                    if (result.success) {
                        location.reload();
                    } else {
                        alert(result.message || "Failed to delete.");
                    }
                }
            });
        }
    });
}

// 7. Child Row Panel Builder (Updated to camelCase)
function formatChildPanel(rowData) {
    let policyHtml = '', nomineeHtml = '', claimHtml = '';

    if (rowData.values) { // camelCase 'values'
        Object.values(rowData.values).forEach(item => {
            let displayValue = item.value || '<em class="text-muted small">Empty</em>';

            if (item.type === 'file' && item.value) {
                const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(item.value);
                displayValue = isImg
                    ? `<a href="${item.value}" target="_blank"><img src="${item.value}" class="img-thumbnail" style="max-height:60px;" /></a>`
                    : `<a href="${item.value}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2">Download File</a>`;
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
            </div>
        </div>`;
}