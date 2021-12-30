namespace LibreStream.Api
{
    public static class TestConstants
	{
		public static readonly string OPM_URL = "https://test.com";

		#region Claims

		public static readonly string DEVICE_CLAIM_TEST = "{\"properties\":{\"connectionString\":\"https\",\"username\":\"osisoft-tst-admin\",\"password\":\"hi\"}}";
		public static readonly string MEASUREMENT_CLAIM_TEST = "{\"properties\":{\"baseAddress\":\"https\",\"username\":\"osisoft-tst-admin\",\"password\":\"hi\"}}";

		#endregion

		#region IoT

		public static readonly string IOT_QUERY_GETEVENT_TEST = "{\"serviceProperties\":null,\"getEvents\":{\"timeSeriesId\":[\"e6700583-59ad-439c-9cd7-e9c7b99bbde3.chiller-01.5\"],\"searchSpan\":{\"from\":\"2020-01-09T22:00:52.6180301Z\",\"to\":\"2020-01-10T23:00:52.6180301Z\"},\"projectedProperties\":[{\"name\":\"temperature\",\"type\":\"Double\"}]}}";
		public static readonly string IOT_QUERY_AGGREGATE_TEST = "{\"serviceProperties\":null,\"aggregateSeries\":{\"timeSeriesId\":[\"e6700583-59ad-439c-9cd7-e9c7b99bbde3.chiller-01.5\"],\"searchSpan\":{\"from\":\"2020-01-12T19:14:52.6180301Z\",\"to\":\"2020-01-12T23:00:52.6180301Z\"},\"interval\":\"PT5M\",\"inlineVariables\":{\"Count\":{\"kind\":\"aggregate\",\"filter\":null,\"aggregation\":{\"tsx\":\"count()\"}},\"MinTemperature\":{\"kind\":\"numeric\",\"value\":{\"tsx\":\"$event.temperature\"},\"filter\":null,\"aggregation\":{\"tsx\":\"min($value)\"}},\"MaxTemperature\":{\"kind\":\"numeric\",\"value\":{\"tsx\":\"$event.temperature\"},\"filter\":null,\"aggregation\":{\"tsx\":\"max($value)\"}}},\"projectedVariables\":[\"Count\",\"MinTemperature\",\"MaxTemperature\"]}}";

		public static readonly string IOT_QUERY_RESULT = "{\"timestamps\":[\"2020-01-12T19:10:00Z\",\"2020-01-12T19:15:00Z\",\"2020-01-12T19:20:00Z\",\"2020-01-12T19:25:00Z\",\"2020-01-12T19:30:00Z\",\"2020-01-12T19:35:00Z\",\"2020-01-12T19:40:00Z\",\"2020-01-12T19:45:00Z\",\"2020-01-12T19:50:00Z\",\"2020-01-12T19:55:00Z\",\"2020-01-12T20:00:00Z\",\"2020-01-12T20:05:00Z\",\"2020-01-12T20:10:00Z\",\"2020-01-12T20:15:00Z\",\"2020-01-12T20:20:00Z\",\"2020-01-12T20:25:00Z\",\"2020-01-12T20:30:00Z\",\"2020-01-12T20:35:00Z\",\"2020-01-12T20:40:00Z\",\"2020-01-12T20:45:00Z\",\"2020-01-12T20:50:00Z\",\"2020-01-12T20:55:00Z\",\"2020-01-12T21:00:00Z\",\"2020-01-12T21:05:00Z\",\"2020-01-12T21:10:00Z\",\"2020-01-12T21:15:00Z\",\"2020-01-12T21:20:00Z\",\"2020-01-12T21:25:00Z\",\"2020-01-12T21:30:00Z\",\"2020-01-12T21:35:00Z\",\"2020-01-12T21:40:00Z\",\"2020-01-12T21:45:00Z\",\"2020-01-12T21:50:00Z\",\"2020-01-12T21:55:00Z\",\"2020-01-12T22:00:00Z\",\"2020-01-12T22:05:00Z\",\"2020-01-12T22:10:00Z\",\"2020-01-12T22:15:00Z\",\"2020-01-12T22:20:00Z\",\"2020-01-12T22:25:00Z\",\"2020-01-12T22:30:00Z\",\"2020-01-12T22:35:00Z\",\"2020-01-12T22:40:00Z\",\"2020-01-12T22:45:00Z\",\"2020-01-12T22:50:00Z\",\"2020-01-12T22:55:00Z\",\"2020-01-12T23:00:00Z\"],\"properties\":[{\"values\":[1,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,12,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0],\"name\":\"Count\",\"type\":\"Long\"},{\"values\":[77.3155954624832,71.3011780358158,71.2731687538434,71.8503998700066,71.3052447466437,71.3256687450109,71.8946877439249,71.3323923619848,71.266051906867,71.2695989606528,71.9058955638464,71.6408394732936,71.4655562130341,71.2996865948894,71.6502434727271,71.283534126139,71.5597018891991,71.4520498866691,71.3077666797013,71.4239577775234,72.5091658410426,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null],\"name\":\"MinTemperature\",\"type\":\"Double\"},{\"values\":[77.3155954624832,78.7374804227089,78.7294627900605,78.5811647527531,78.626270036854,78.533009912252,78.4940713514779,78.6563821497403,77.9899839529488,78.7346648715831,78.682678530427,78.3529228610466,78.5686284465802,78.683087567302,78.5580304520242,78.5945419000204,78.1764293866821,78.6908509558723,78.7496162136083,78.6689451452899,77.3655736952161,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null],\"name\":\"MaxTemperature\",\"type\":\"Double\"}],\"progress\":100.0}";

		#endregion
	}
}
