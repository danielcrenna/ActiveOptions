using System.Threading.Tasks;
using Demo;
using TestKitchen;
using TestKitchen.TestAdapter;

namespace ActiveOptions.Tests
{
	public class ConfigurationControllerTests : WebTest<Startup>
	{
		[Fact]
		public async Task Get_configuration()
		{
			var response = await Client.GetAsync("/config");
			response.EnsureSuccessStatusCode();
		}
	}
}
