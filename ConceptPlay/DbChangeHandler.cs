using ConceptPlay.Pages;
using DLL_ConnectionToDatabase;

namespace ConceptPlay
{
    static class DbChangeHandler
    {
        public static async void ProcessDbUpdates( 
            MainWindow window, AppDbContext dbContext,
            string? table, string? action, int id)
        {
            if (table is null || action is null || id == 0)
                return;

            var page = window.CurrentPage;

            switch (page)
            {
                case PageConcept pageConcept:
                    await HandleConceptPageChangeAsync(
                        window, dbContext, pageConcept, 
                        table, id, action);
                    break;

                case PageUser pageUser:
                    await HandleUserPageChangeAsync(
                        window, dbContext, pageUser, 
                        table, id, action);
                    break;
            }
        }

        private static async Task HandleConceptPageChangeAsync(
            MainWindow window, AppDbContext dbContext,
            PageConcept page, string table, 
            int id, string action)
        {
            switch (table)
            {
                case "Concept":
                    if (page.SelectedConcept!.Id == id)
                    {
                        if (action == "Delete")
                        {
                            await window.GoToPageAsync(
                                new PageHub(window));
                        }
                        else
                        {
                            await page.InitAsync(
                                page.SelectedConcept!.Id);
                        }
                    }

                    break;

                case "Comment":
                    var comment = await DbMenu
                        .GetByIdAsync<Comment>(id, dbContext);

                    if (page.SelectedConcept!.Id == comment?.ConceptId)
                    {
                        page.DbContext = dbContext;
                        await page.SetCommentsAsync();
                    }

                    break;
            }
        }

        private static async Task HandleUserPageChangeAsync(
            MainWindow window, AppDbContext dbContext,
            PageUser page, string table, 
            int id, string action)
        {
            switch (table)
            {
                case "UserInfo":
                    if (page.SelectedUser!.Id == id)
                    {
                        if (action == "Delete")
                        {
                            await window.GoToPageAsync(
                                new PageHub(window));
                        }
                        else
                        {
                            await page.InitAsync(
                                page.SelectedUser!.Id);
                        }
                    }

                    break;

                case "Concept":
                    var concept = await DbMenu
                        .GetByIdAsync<Concept>(id, dbContext);

                    if (page.SelectedUser!.Id == concept?.UserId)
                    {
                        page.DbContext = dbContext;
                        await page.LoadUserConceptsAsync();
                    }

                    break;
            }
        }
    }
}
