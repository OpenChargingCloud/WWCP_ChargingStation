


var ConnectionColors = {};

function GetConnectionColors(ConnectionId) {

    var Colors = ConnectionColors[ConnectionId];

    if (Colors != undefined)
        return Colors;

    else {

        var red   = Math.floor(Math.random() * 80 + 165).toString(16);
        var green = Math.floor(Math.random() * 80 + 165).toString(16);
        var blue  = Math.floor(Math.random() * 80 + 165).toString(16);

        var ConnectionColor = red + green + blue;

        ConnectionColors[ConnectionId]            = new Object();
        ConnectionColors[ConnectionId].textcolor  = "000000";
        ConnectionColors[ConnectionId].background = ConnectionColor;

        return ConnectionColors[ConnectionId];

    }

}

function CreateLogEntry(Timestamp, RoamingNetwork, Command, Message, ConnectionColorKey) {

    var ConnectionColor   = GetConnectionColors(ConnectionColorKey);

    var div = document.createElement('div');
    div.className         = "LogLine";
    div.style.color       = "#" + ConnectionColor.textcolor;
    div.style.background  = "#" + ConnectionColor.background;
    div.innerHTML         = "<span class=\"Timestamp\">" + new Date(Timestamp).format('dd.mm.yyyy HH:MM:ss') + "</span>" +
                            "<span class=\"RoamingNetwork\">" + RoamingNetwork + "</span>" +
                            "<span class=\"OnNewConnection\">" + Command + "</span>" +
                            "<span class=\"OnNewConnectionMessage\">" + Message + "</span>";

    if (div.innerHTML.indexOf(StreamFilterPattern.value) > -1)
        div.style.display = 'block';
    else
        div.style.display = 'none';

    document.getElementById('EventsDiv').insertBefore(div, document.getElementById('EventsDiv').firstChild);

}

function AppendLogEntry(Timestamp, RoamingNetwork, Command, SearchPattern, Message) {

    var AllLogLines = document.getElementById('EventsDiv').getElementsByClassName('LogLine');

    for (var i = 0; i < AllLogLines.length; i++)
    {
        if (AllLogLines[i].getElementsByClassName("OnNewConnection")[0].innerHTML == Command)
        {
            if (AllLogLines[i].innerHTML.indexOf(SearchPattern) > -1)
            {
                AllLogLines[i].getElementsByClassName("OnNewConnectionMessage")[0].innerHTML += Message;
                break;
            }
        }
    }

}



function DebugLogs() {

    var LogEventsSource = null;

    if (window.EventSource !== undefined) {
        LogEventsSource = new EventSource('/DebugLog');
        document.getElementById('EventSourceType').firstChild.innerHTML = 'Browser EventSource';
    } else {
        LogEventsSource = new EventSource2('/LogEvents');
        document.getElementById('EventSourceType').firstChild.innerHTML = 'JavaScript EventSource';
    }

    LogEventsSource.onmessage = function (event) {

        LastEventId = event.lastEventId;

        var div = document.createElement('div');
        div.innerHTML = "Message: " + event.data + "<br>";

        document.getElementById('EventsDiv').insertBefore(div, document.getElementById('EventsDiv').firstChild);

    };


    LogEventsSource.addEventListener('error',              function (event) {

        if (event.readyState == EventSource.CLOSED) {
            // Connection was closed.
        }

        if (event.data !== undefined) {

            var div = document.createElement('div');
            div.innerHTML = "Error: " + event.data + "<br>";

            document.getElementById('EventsDiv').insertBefore(div, document.getElementById('EventsDiv').firstChild);

        }

    }, false);

    LogEventsSource.addEventListener('OnStarted',          function (event) {

        try {

            var data = JSON.parse(event.data);

            var Timestamp    = new Date(data.Timestamp).format('dd.mm.yyyy HH:MM:ss');
            var Message      = data.Message;
            var LastEventId  = event.lastEventId;

            var div = document.createElement('div');
            div.className    = "LogLine";
            div.innerHTML    = "<span class=\"Timestamp\">" + Timestamp + "</span>" +
                               "<span class=\"OnStarted\">&nbsp;</span>" +
                               "<span class=\"OnStartedMessage\">" + Message + "</span>";

            if (div.innerHTML.indexOf(StreamFilterPattern.value) > -1)
                div.style.display = 'block';
            else
                div.style.display = 'none';

            document.getElementById('EventsDiv').insertBefore(div, document.getElementById('EventsDiv').firstChild);

        }
        catch (ex) {
        }

    }, false);

    LogEventsSource.addEventListener('InvalidJSONRequest', function (event) {

        try {

            var data = JSON.parse(event.data);

            var Timestamp         = new Date(data.Timestamp).format('dd.mm.yyyy HH:MM:ss');
            var RemoteSocket      = data.RemoteSocket;
            var RoamingNetwork    = data.RoamingNetwork;
            var EVSEId            = data.EVSEId;
            var LastEventId       = event.lastEventId;
            var ConnectionColor   = GetConnectionColors(RemoteSocket);

            var div = document.createElement('div');
            div.className         = "LogLine";
            div.style.color       = "#" + ConnectionColor.textcolor;
            div.style.background  = "#" + ConnectionColor.background;
            div.innerHTML         = "<span class=\"Timestamp\">" + Timestamp + "</span>" +
                                    "<span class=\"OnNewConnection\">" + RemoteSocket + "</span>" +
                                    "<span class=\"OnNewConnectionMessage\">Invalid JSONRequest</span>";

            if (div.innerHTML.indexOf(StreamFilterPattern.value) > -1)
                div.style.display = 'block';
            else
                div.style.display = 'none';

            document.getElementById('EventsDiv').insertBefore(div, document.getElementById('EventsDiv').firstChild);

        }
        catch (ex) {
        }

    }, false);


    LogEventsSource.addEventListener('AUTHSTARTRequest',   function (event) {

        try {

            var data  = JSON.parse(event.data);

            CreateLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           "AUTHSTART",
                           "'" + data.AuthToken + "' at " + data.EVSEId + " / " + data.OperatorId + " <span class=\"hidden\">(PartnerSessionId " + data.PartnerSessionId + ")</span>",
                           data.PartnerSessionId // ConnectionColorKey
                          );

        }
        catch (ex) {
        }

    }, false);

    LogEventsSource.addEventListener('AUTHSTARTResponse',  function (event) {

        try {

            var data     = JSON.parse(event.data);
            var Message  = " => " + data.Description;

            Message += (data.ProviderId     != "") ? " by '"        + data.ProviderId     + "'" : "";
            Message += (data.AuthorizatorId != "") ? " via '"       + data.AuthorizatorId + "'" : "";
            Message += (data.SessionId != "") ? " <span class=\"hidden\">(SessionId " + data.SessionId + ")</span>" : "";
            Message += " [" + data.Runtime + " ms]";

            AppendLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           // 1) Search for a logline with this command
                           "AUTHSTART",
                           // 2) Search for a logline with this pattern
                           "(PartnerSessionId " + data.PartnerSessionId + ")",
                           Message);

        }
        catch (ex) {
        }

    }, false);


    LogEventsSource.addEventListener('AUTHSTOPRequest',    function (event) {

        try {

            var data  = JSON.parse(event.data);

            CreateLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           "AUTHSTOP",
                           "'" + data.AuthToken + "' at " + data.EVSEId + " / " + data.OperatorId + " <span class=\"hidden\">(PartnerSessionId " + data.PartnerSessionId + ")</span>",
                           data.PartnerSessionId // ConnectionColorKey
                          );

        }
        catch (ex) {
        }

    }, false);

    LogEventsSource.addEventListener('AUTHSTOPResponse',   function (event) {

        try {

            var data     = JSON.parse(event.data);
            var Message  = " => " + data.Description;

            Message += (data.ProviderId     != "") ? " by '"        + data.ProviderId     + "'" : "";
            Message += (data.AuthorizatorId != "") ? " via '"       + data.AuthorizatorId + "'" : "";
            Message += (data.SessionId      != "") ? " <span class=\"hidden\">(SessionId " + data.SessionId      + ")</span>" : "";
            Message += " [" + data.Runtime + " ms]";

            AppendLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           // 1) Search for a logline with this command
                           "AUTHSTOP",
                           // 2) Search for a logline with this pattern
                           "(PartnerSessionId " + data.PartnerSessionId + ")",
                           Message);

        }
        catch (ex) {
        }

    }, false);


    LogEventsSource.addEventListener('OnRemoteEVSEStart', function (event) {

        try {

            var data  = JSON.parse(event.data);

            CreateLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           "REMOTESTART",
                           "eMA-Id " + data.eMAId + " at EVSE " + data.EVSEId + " / " + data.ProviderId + " <span class=\"hidden\">(EventTrackingId " + data.EventTrackingId + ")</span>",
                           data.SessionId // ConnectionColorKey
                          );

        }
        catch (ex) {
        }

    }, false);

    LogEventsSource.addEventListener('OnRemoteEVSEStarted', function (event) {

        try {

            var data    = JSON.parse(event.data);
            var Message = " => " + data.Result + " " + (data.Description != null ? data.Description : "") + " [" + data.Runtime + " ms]";

            AppendLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           // 1) Search for a logline with this command
                           "REMOTESTART",
                           // 2) Search for a logline with this pattern
                           "(EventTrackingId " + data.EventTrackingId + ")",
                           Message != null ? Message : ":P");

        }
        catch (ex) {
        }

    }, false);


    LogEventsSource.addEventListener('OnRemoteStop', function (event) {

        try {

            var data  = JSON.parse(event.data);

            CreateLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           "REMOTESTOP",
                           "EVSE " + data.EVSEId + " / " + data.ProviderId + " <span class=\"hidden\">(EventTrackingId " + data.EventTrackingId + ")</span>",
                           data.SessionId // ConnectionColorKey
                          );

        }
        catch (ex) {
        }

    }, false);

    LogEventsSource.addEventListener('OnRemoteStopped', function (event) {

        try {

            var data = JSON.parse(event.data);
            var Message = " => " + data.Result + " " + (data.Description != null ? data.Description : "") + " [" + data.Runtime + " ms]";

            AppendLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           // 1) Search for a logline with this command
                           "REMOTESTOP",
                           // 2) Search for a logline with this pattern
                           "(EventTrackingId " + data.EventTrackingId + ")",
                           Message);

        }
        catch (ex) {
        }

    }, false);



    LogEventsSource.addEventListener('SENDCDRRequest',     function (event) {

        try {


            var data       = JSON.parse(event.data);
            var EVDriver   = "";

            if (data.hasOwnProperty('UID'))
                EVDriver   = "UID '" + data.UID + "'";

            else if (data.hasOwnProperty('eMAId'))
                EVDriver   = "eMA-Id '" + data.eMAId + "'";

            var MeterValue = data.MeterValueEnd - data.MeterValueStart;

            //var SessionStart        = data.SessionStart;
            //var ChargeStart         = data.ChargeStart;
            //var ChargeEnd           = data.ChargeEnd;
            //var SessionEnd          = data.SessionEnd;

            CreateLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           "SENDCDR",
                           EVDriver + " at EVSE " + data.EVSEId + " consumed " + MeterValue + " kWh of '" + data.PartnerProductId + "' <span class=\"hidden\">(SessionIds " + data.SessionId + " / " + data.PartnerSessionId + ")</span>",
                           data.PartnerSessionId // ConnectionColorKey
                          );

        }
        catch (ex) {
        }

    }, false);


    LogEventsSource.addEventListener('OnEVSEStatusChanged', function (event) {

        try {

            var data = JSON.parse(event.data);

            CreateLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           "EVSE status changed",
                           "EVSE " + data.EVSEId + " / " + data.NewStatus + " <span class=\"hidden\">(SessionId " + data.SessionId + ")</span>",
                           data.SessionId // ConnectionColorKey
                          );

        }
        catch (ex) {
        }

    }, false);






    LogEventsSource.addEventListener('UPDATEEVSEStatesRequest', function (event) {

        try {

            var data        = JSON.parse(event.data);
            var EVSEStates  = "";

            if (data.hasOwnProperty('Values'))
                for (var PropertyKey in data.Values) {
                    EVSEStates += PropertyKey;
                    EVSEStates += " = ";
                    EVSEStates += data.Values[PropertyKey];
                    EVSEStates += "; ";
                }

            CreateLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           "UPDATE EVSE States",
                           EVSEStates,
                           data.Timestamp // ConnectionColorKey
                          );

        }
        catch (ex) {
        }

    }, false);

    LogEventsSource.addEventListener('UPDATEEVSEStatesResponse', function (event) {

        try {

            var data        = JSON.parse(event.data);
            var Message     = " => " + data.Description + " / " + data.AdditionalInfo;
            var EVSEStates  = "";

            if (data.hasOwnProperty('Values'))
                for (var PropertyKey in data.Values) {
                    EVSEStates += PropertyKey;
                    EVSEStates += " = ";
                    EVSEStates += data.Values[PropertyKey];
                    EVSEStates += "; ";
                }

            AppendLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           // 1) Search for a logline with this command
                           "UPDATE EVSE States",
                           // 2) Search for a logline with this pattern
                           EVSEStates,
                           Message);

        }
        catch (ex) {
        }

    }, false);

    LogEventsSource.addEventListener('REMOVEEVSEStatesRequest', function (event) {

        try {

            var data        = JSON.parse(event.data);
            var EVSEStates  = "";

            if (data.hasOwnProperty('Values'))
                for (var EVSEId in data.Values) {
                    EVSEStates += EVSEId;
                    EVSEStates += "; ";
                }

            CreateLogEntry(data.Timestamp,
                           data.RoamingNetwork,
                           "REMOVE EVSE States",
                           EVSEStates,
                           data.Timestamp // ConnectionColorKey
                          );

        }
        catch (ex) {
        }

    }, false);



    LogEventsSource.addEventListener('OnError', function (event) {

        try {

            var data = JSON.parse(event.data);

            var Timestamp = new Date(data.Timestamp).format('dd.mm.yyyy HH:MM:ss');
            var ConnectionId = data.ConnectionId;
            var Error = data.Error;
            var CurrentBuffer = data.CurrentBuffer;
            var LastEventId = event.lastEventId;

            var div = document.createElement('div');
            div.className = "LogLine";
            div.style.color = "#" + ConnectionColors[ConnectionId].textcolor;
            div.style.background = "#" + ConnectionColors[ConnectionId].background;
            div.innerHTML = "<span class=\"Timestamp\">" + Timestamp + "</span>" +
                                    "<span class=\"OnError\">" + ConnectionId + "</span>" +
                                    "<span class=\"OnErrorMessage\">" + Error + "<br>Current buffer: " + CurrentBuffer + "</span>";

            if (div.innerHTML.indexOf(StreamFilterPattern.value) > -1)
                div.style.display = 'block';
            else
                div.style.display = 'none';

            document.getElementById('EventsDiv').insertBefore(div, document.getElementById('EventsDiv').firstChild);

        }
        catch (ex) {
        }

    }, false);


}
