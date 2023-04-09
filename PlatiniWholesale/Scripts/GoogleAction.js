var googleUser = {};
var startApp = function () {
    gapi.load('auth2', function () {
        // Retrieve the singleton for the GoogleAuth library and set up the client.
        auth2 = gapi.auth2.init({
            client_id: $("meta[name=gi]").attr('content'),
            cookiepolicy: 'single_host_origin',
            // Request scopes in addition to 'profile' and 'email'
            //scope: 'additional_scope'
        });
        var x = document.querySelectorAll('.customBtn');
        for (i = 0; i < x.length; i++)
        {
            attachSignin(x[i], x[i].getAttribute("data-wholesale"));
        }
    });
};

function attachSignin(element,isWholeSale) {
    //console.log(element.id);
    auth2.attachClickHandler(element, {},
        function (googleUser) {
            onSignIn(auth2.currentUser.get(), isWholeSale);
        }, function (error) {
            alert(JSON.stringify(error, undefined, 2));
        });
}



function onSignIn(googleUser, isWholeSale) {
    var profile = googleUser.getBasicProfile();
    $.ajax({
        type: "POST",
        contentType: "application/json",
        url: '/home/GoogleLogin',
        data: JSON.stringify({ id: profile.getId(), Name: profile.getName(), email: profile.getEmail(), isWholeSale: isWholeSale }),
        dataType: "json",
        success: function (data) {
            console.log(data);
            if (data.isBill == false)
                window.location = window.location;
            else
                window.location = '/home/billing';
        }
    });
}
