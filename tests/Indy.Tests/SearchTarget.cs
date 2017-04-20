namespace Indy.Tests
{
    enum SearchTargetEnum
    {
        ValueOne
    }

    delegate int SearchTargetDelegate(int x);

    class SearchTarget
    {
        int searchTargetField;
        static string searchTargetFieldStatic;

        void SearchTargetMethod() { }
        static int SearchTargetMethodStatic()
        {
            return 5;
        }

        int SearchTargetProperty { get; set; }
        static string SearchTargetPropertyStatic { get; set; }

        event SearchTargetDelegate SearchTargetEvent;
        static event SearchTargetDelegate SearchTargetEventStatic;
    }
}