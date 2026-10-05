using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.iOS;

internal class SingleViewLifetime : ISingleViewApplicationLifetime, ISingleTopLevelApplicationLifetime
{
    private Control? _mainView;
    private CornerstoneView? _view;

    public CornerstoneView? View
    {
        get => _view;
        internal set
        {
            if (_view != null)
            {
                _view.Content = null;
                _view.Dispose();
            }
            _view = value;
            _view?.Content = _mainView;
        }
    }

    public Control? MainView
    {
        get => _mainView;
        set
        {
            if (_mainView != value)
            {
                _mainView = value;
                _view?.Content = _mainView;
            }
        }
    }

    public TopLevel? TopLevel => View?.TopLevel;
}
