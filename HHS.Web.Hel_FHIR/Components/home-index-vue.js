$(document).ready(function () {

    var app = new Vue({
        el: '#vue-app',
        data: {
            moment: moment,
            patientMrn: 33789,
            personSearchValue: null,
            accessToken: '',
            feedbackMessage: '',
            retrievingToken: false,
            tokenRetrieved: false,
            patientDetails: null,
            patientLoading: false,
            immunizations: null,
            immunizationsLoading: false,
            conditions: null,
            conditionsLoading: false,
            encounters: null,
            encountersLoading: false,
            labs: null,
            labsLoading: false,
            vitals: null,
            vitalsLoading: false,
            socials: null,
            socialsLoading: false,
            consents: null,
            consentsLoading: false,
            medications: null,
            medicationsLoading: false,
            errorMessage: '',
            errorMessages: [],
        },
        mounted: function () {

        },
        methods: {
            GetPatientProfile: function () {
                var self = this;

                self.retrievingToken = true;
                self.feedbackMessage = 'Retrieving access token...'

                axios.get('/api/hel/get-access-token')
                    .then(function (tokenResponse) {
                        self.accessToken = tokenResponse.data.access_token;
                        self.tokenRetrieved = true;

                        self.patientLoading = true;
                        self.feedbackMessage = 'Loading patient details...';


                        var parameters = {
                            accessToken: self.accessToken,
                            patientMrn: self.patientMrn
                        };


                        axios.post('/api/hel/patient-details', parameters)
                            .then(function (patientResponse) {
                                self.patientLoading = false;
                                self.patientDetails = patientResponse.data;

                                if (self.patientDetails != null) {

                                    self.consentsLoading = true;
                                    self.encountersLoading = true;
                                    self.conditionsLoading = true;
                                    self.labsLoading = true;
                                    self.vitalsLoading = true;
                                    self.socialsLoading = true;
                                    self.immunizationsLoading = true;
                                    self.medicationsLoading = true;

                                    axios.post('/api/hel/patient-consents', parameters)
                                        .then(function (consentsResponse) {
                                            self.consentsLoading = false;
                                            self.consents = consentsResponse.data;
                                        });

                                    axios.post('/api/hel/patient-encounters', parameters)
                                        .then(function (encounterResponse) {
                                            self.encountersLoading = false;
                                            self.encounters = encounterResponse.data;
                                        });

                                    axios.post('/api/hel/patient-conditions', parameters)
                                        .then(function (conditionResponse) {
                                            self.conditionsLoading = false;
                                            self.conditions = conditionResponse.data;
                                        });

                                    axios.post('/api/hel/patient-medications', parameters)
                                        .then(function (medicationResponse) {
                                            self.medicationsLoading = false;
                                            self.medications = medicationResponse.data;
                                        });

                                    axios.post('/api/hel/patient-labs', parameters)
                                        .then(function (labResponse) {
                                            self.labsLoading = false;
                                            self.labs = labResponse.data;
                                        }).catch(function (err) {
                                            self.errorMessages.push({ 'resource': 'Labs', 'errorName': err.name, 'errorMessage': err.message });
                                            self.labsLoading = false;
                                        });

                                    axios.post('/api/hel/patient-vitals', parameters)
                                        .then(function (vitalsResponse) {
                                            self.vitalsLoading = false;
                                            self.vitals = vitalsResponse.data;
                                        });

                                    axios.post('/api/hel/patient-socials', parameters)
                                        .then(function (socialsResponse) {
                                            self.socialsLoading = false;
                                            self.socials = socialsResponse.data;
                                        });

                                    axios.post('/api/hel/patient-immunizations', parameters)
                                        .then(function (immunizationResponse) {
                                            self.immunizationsLoading = false;
                                            self.immunizations = immunizationResponse.data;
                                        });

                                    self.retrievingToken = false;
                                    self.feedbackMessage = '';
                                }
                            });




                    })
                    .catch(function (err) {
                        self.errorMessages.push({ 'resource': 'Token', 'errorName': err.name, 'errorMessage': err.message });
                        self.retrievingToken = false;
                        self.feedbackMessage = '';
                    });
            },
            GetPatientProfileFromSearch: function () {
                var self = this;
            },
            LaunchEntryDetails: function (fhirUrl) {
                var self = this;

                console.log(fhirUrl);

                var parameters = {
                    accessToken: self.accessToken,
                    fhirUrl: fhirUrl
                };


                axios.post('/api/hel/entry-details', parameters).then(function (response) {

                    //$.fancybox.open({
                    //    src: url,
                    //    type: 'iframe',
                    //    opts: {
                    //        caption: 'Entry Details',
                    //        smallBtn: true,
                    //    }
                    //});

                });
            }
        }

    });



    $("#person-search").autocomplete({
        source: function (request, response) {
            $.ajax({
                method: 'GET',
                url: "/api/person/search?query=" + request.term,
                success: function (data) {
                    //console.log(data);
                    response(data);
                }
            });
        },
        minLength: 3,
        select: function (event, ui) {

            $('#person-search').text(ui.item.value);
            app.patientMrn = ui.item.id;
            app.GetPatientProfile();

            //patientMrn = ui.item.id;
            //console.log(patientMrn);


        }
    }).autocomplete("instance")._renderItem = function (ul, item) {
        if (item.dob) {
            return $("<li>")
                .append("<div>" + item.id + ": " + item.value + " " + moment(item.dob).format('YYYY-MM-DD') + "</div>")
                .appendTo(ul);
        }
        else {
            return $("<li>")
                .append("<div>" + item.id + ": " + item.value + "</div>")
                .appendTo(ul);
        }
    };
});





