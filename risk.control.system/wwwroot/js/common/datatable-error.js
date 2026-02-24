const DataTableErrorHandler = function (xhr, status, error) {
    console.error("AJAX Error:", status, error);
    console.error("Response:", xhr.responseText);

    if (xhr.status === 401 || xhr.status === 403) {
        $.confirm({
            title: 'Session Expired!',
            content: 'Your session has expired or you are unauthorized. You will be redirected to the login page.',
            type: 'red',
            typeAnimated: true,
            buttons: {
                Ok: {
                    text: 'Login',
                    btnClass: 'btn-red',
                    action: () => window.location.href = '/Account/Login'
                }
            },
            onClose: () => window.location.href = '/Account/Login'
        });
    }
    else if (xhr.status === 400) {
        $.confirm({
            title: 'Bad Request!',
            content: 'Try with valid data. You will be redirected to the Dashboard page.',
            type: 'orange',
            typeAnimated: true,
            buttons: {
                Ok: () => window.location.href = '/Dashboard/Index'
            },
            onClose: () => window.location.href = '/Dashboard/Index'
        });
    } else {
        $.confirm({
            title: 'Server Error!',
            content: 'An unexpected server error occurred. You will be redirected to the Dashboard page.',
            type: 'orange',
            typeAnimated: true,
            buttons: {
                Ok: () => window.location.href = '/Dashboard/Index'
            },
            onClose: () => window.location.href = '/Dashboard/Index'
        });
    }
};