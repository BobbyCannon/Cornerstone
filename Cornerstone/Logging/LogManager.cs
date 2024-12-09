#region References

using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Managers;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Logging;

public class LogManager : ViewManager<LogEventView>
{
	#region Constructors

	public LogManager(
		IDateTimeProvider dateTimeProvider,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher)
		: base(dateTimeProvider, dependencyProvider, dispatcher, (_, _) => false, new OrderBy<LogEventView>(x => x.MessagedOn))
	{
		Limit = 100;
	}

	#endregion
}