$(document).ready(function () {
    // 1. Core structural columns for the main overview table
    const columnsConfig = [
        {
            className: 'dt-control align-middle text-center',
            orderable: false,
            data: null,
            defaultContent: '',
            width: '60px'
        },
        {
            data: 'fields',
            title: 'Policy #',
            className: 'align-middle fw-bold',
            render: function (data) {
                const field = data.find(f => f.label.toLowerCase().includes('policy number'));
                return field ? field.value : '<span class="text-muted">-</span>';
            }
        },
        {
            data: 'fields',
            title: 'LA  Name',
            className: 'align-middle',
            width: '100px',
            render: function (data) {
                const field = data.find(f => f.label.toLowerCase() === 'life-assured name');
                return field ? field.value : '<span class="text-muted">-</span>';
            }
        },
        {
            data: 'fields',
            title: 'LA  Photo',
            className: 'align-middle text-center table-profile-image',
            orderable: false,
            render: function (data) {
                return renderPhotoCell(data, 'Life-assured photo');
            }
        },
        // NEW: Policy Document Column
        {
            data: 'fields',
            title: 'Policy Document',
            className: 'align-middle text-center',
            orderable: false,
            render: function (data) {
                return renderDocumentCell(data, 'Policy Document');
            }
        },
        // NEW: Claim Document Column
        {
            data: 'fields',
            title: 'Claim Document',
            className: 'align-middle text-center',
            orderable: false,
            render: function (data) {
                return renderDocumentCell(data, 'Claim Document');
            }
        },
        {
            data: 'fields',
            title: 'Nominee Name',
            className: 'align-middle',
            width: '100px',
            render: function (data) {
                const field = data.find(f => f.label.toLowerCase() === 'nominee name');
                return field ? field.value : '<span class="text-muted">-</span>';
            }
        },
        {
            data: 'fields',
            title: 'Nominee Photo',
            className: 'align-middle text-center table-profile-image',
            orderable: false,
            render: function (data) {
                return renderPhotoCell(data, 'Nominee Photo');
            }
        },
        {
            data: 'submittedAt',
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
            data: 'id',
            title: 'Actions',
            orderable: false,
            searchable: false,
            className: 'align-middle text-center text-nowrap',
            width: '130px',
            render: function (data) {
                return `
                    <a href="/Claim/EditForm/${data}" class="btn btn-sm btn-outline-primary px-2 py-1 me-1"><i class="bi bi-pencil-square"></i> Edit</a>
                    <button type="button" class="btn btn-sm btn-outline-danger px-2 py-1 btn-delete" data-id="${data}"><i class="bi bi-trash"></i> Delete</button>
                `;
            }
        }
    ];

    // 2. Initialize DataTable
    const table = $('#submissionsTable').DataTable({
        ajax: {
            url: '/Claim/GetSubmissionsJson',
            type: 'GET'
        },
        columns: columnsConfig,
        order: [[3, 'desc']], // Sort by Date Submitted
        autoWidth: false
    });

    // 3. Row expand listener
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

    // 4. AJAX Delete
    $('#submissionsTable tbody').on('click', '.btn-delete', function () {
        const id = $(this).data('id');
        if (confirm("Are you sure you want to permanently delete this submission record?")) {
            $.ajax({
                url: `/Claim/DeleteSubmission/${id}`,
                type: 'POST',
                success: function (result) {
                    if (result.success) table.ajax.reload(null, false);
                    else alert(result.message || "Failed to delete.");
                }
            });
        }
    });
});
function renderPhotoCell(data, fieldLabel) {
    if (!data) return '<span class="text-muted small">-</span>';

    const field = data.find(f => f.label.toLowerCase() === fieldLabel.toLowerCase());
    if (!field || !field.value) {
        return '<span class="text-muted small">-</span>';
    }

    return `<a href="${field.value}" target="_blank">
                <img src="${field.value}" class="img-thumbnail table-profile-image"  alt="Nominee" />
            </a>`;
}
function renderDocumentCell(data, fieldLabel) {
    if (!data) return '<span class="text-muted small">-</span>';

    const field = data.find(f => f.label.toLowerCase() === fieldLabel.toLowerCase());
    if (!field || !field.value) {
        return '<span class="text-muted small">-</span>';
    }

    const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(field.value);
    if (isImg) {
        return `<a href="${field.value}" target="_blank">
                    <img src="${field.value}" class="img-thumbnail submissions-thumbnail" style="max-height: 40px;" />
                </a>`;
    } else {
        return `<a href="${field.value}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2 small text-nowrap">
                    <i class="bi bi-download"></i> View
                </a>`;
    }
}
// Helper function to build a side-by-side structured dashboard inside the expanded row
function formatChildPanel(rowData) {
    let policyHtml = '', nomineeHtml = '', claimHtml = '';

    rowData.fields.forEach(field => {
        let displayValue = field.value || '<em class="text-muted small">Empty</em>';

        // Handle media attachments/images inline elegantly
        if (field.type === 'file' && field.value) {
            const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(field.value);
            displayValue = isImg
                ? `<a href="${field.value}" target="_blank"><img src="${field.value}" class="img-thumbnail" style="max-height:60px;" /></a>`
                : `<a href="${field.value}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2">Download File</a>`;
        }

        const block = `
            <div class="row py-1 border-bottom mx-0">
                <div class="col-sm-5 fw-semibold text-secondary small">${field.label}</div>
                <div class="col-sm-7 text-dark text-break">${displayValue}</div>
            </div>`;

        if (field.section === 'Nominee') nomineeHtml += block;
        else if (field.section === 'ClaimDetail') claimHtml += block;
        else policyHtml += block;
    });

    return `
        <div class="p-3 bg-light border rounded shadow-sm m-2">
            <div class="row">
                <div class="col-md-4 border-end">
                    <h6 class="text-primary border-bottom pb-2 fw-bold">Policy Info</h6>
                    ${policyHtml || '<p class="text-muted small">No data</p>'}
                </div>
                <div class="col-md-4 border-end">
                    <h6 class="text-success border-bottom pb-2 fw-bold">Nominee Info</h6>
                    ${nomineeHtml || '<p class="text-muted small">No data</p>'}
                </div>
                <div class="col-md-4">
                    <h6 class="text-danger border-bottom pb-2 fw-bold">Claim Info</h6>
                    ${claimHtml || '<p class="text-muted small">No data</p>'}
                </div>
            </div>
        </div>`;
}