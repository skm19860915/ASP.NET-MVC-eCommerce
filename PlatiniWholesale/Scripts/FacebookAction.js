window.fbAsyncInit = function () {
    FB.init({
        appId: $("meta[name=FBId]").attr('content'), // App ID,
        status: true, // check login status
        cookie: true, // enable cookies to allow the server to access the session
        xfbml: true  // parse XFBML
    });
};
(function (d) {
    var js, id = 'facebook-jssdk', ref = d.getElementsByTagName('script')[0];
    if (d.getElementById(id)) { return; }
    js = d.createElement('script'); js.id = id; js.async = true;
    js.src = "//connect.facebook.net/en_US/all.js";
    ref.parentNode.insertBefore(js, ref);
}(document));

function CallFacebookLogin(isWholeSale) {
    FB.getLoginStatus(function (response) {
        if (response.status === 'connected') {
            var uid = response.authResponse.userID;
            var accessToken = response.authResponse.accessToken;
            CallSite(uid, isWholeSale);
        } else if (response.status === 'not_authorized') {
            if (confirm("do you wish to login")) {
                loginTofb(isWholeSale);
            }
        } else {
            loginTofb(isWholeSale);
        }
    });
}

function loginTofb(isWholeSale) {
    FB.login(function (response) {
        if (response.authResponse && response.authResponse.userID) {
            CallSite(response.authResponse.userID, isWholeSale);
        } else {
            //alert('User cancelled login or did not fully authorize.');
        }
    }, { scope: 'email,user_location' });
};

function CallSite(uid, isWholeSale) {
    FB.api('/' + uid + '?fields=first_name,last_name,email,location{location}', function (response) {
        response.isWholeSale = isWholeSale;
        console.log(response);
        $.ajax({
            type: "POST",
            contentType: "application/json",
            url: '/home/FacebookLogin',
            data: JSON.stringify(response),
            dataType: "json",
            success: function (data) {
                console.log(data);
                if (data.isBill == false)
                    window.location = window.location;
                else
                    window.location = '/home/billing';
            }
        });
    });
}