using LoadingVacancies.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace LoadingVacancies.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Vacancy> vacancies;
        public ObservableCollection<Vacancy> Vacancies
        {
            get { return vacancies; }
            set
            {
                vacancies = value;
                OnPropertyChanged();
            }
        }
        
        public MainViewModel()
        {
            vacancies = new ObservableCollection<Vacancy>();
            Loading_Vacancies();
        }

        public async void Loading_Vacancies()
        {
            try
            {
                string webAddress = @"https://proglib.io/vacancies/all?workType=all&workPlace=all&experience=&salaryFrom=&";
                int countPages = 0;
                using HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(webAddress);
                HttpContent content = response.Content;
                var htmlBody = await content.ReadAsStringAsync();

                //патерн поиска кол-ва страниц
                string pattern = "data-total=\"(.+)\"";
                Regex regexPages = new Regex(pattern);
                MatchCollection matchesPages = regexPages.Matches(htmlBody);
                if (matchesPages == null || matchesPages.Count > 1)
                {
                    MessageBox.Show("Ошибка! Невозможно загрузить страницы!");
                    return;
                }
                foreach (Match matchPage in matchesPages)
                {
                    countPages = int.Parse(matchPage.Groups[1].Value);
                }

                string[] pages = new string[countPages];
                for (int i = 0; i < countPages; i++)
                {
                    pages[i] = webAddress + $"page={i+1}";
                }

                var tasks = pages.Select(page => client.GetStringAsync(page));
                string[] pagesContent = await Task.WhenAll(tasks);

                //патерн поиска адреса, наименования и даты размещения вакансии
                pattern = "href=\"(.+)\" class=\"no-link\">\\s*<.+>\\s*<h2\\s*.+\\s*itemprop=\"title\">(.+)<\\/h2>\\s*<\\/div>\\s*<.+>\\s*<.+>\\s*<div itemprop=\"datePosted\">(.+)<\\/div>";
                Regex regex = new Regex(pattern);
                MatchCollection matches;
                for (int i = 0; i < pagesContent.Length; i++)
                {
                    matches = regex.Matches(pagesContent[i]);
                    foreach (Match match in matches)
                    {
                        vacancies.Add(new Vacancy() { Name = match.Groups[2].ToString(), Email = match.Groups[1].ToString(), Date = match.Groups[3].ToString() });
                    }
                }              
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }                       
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string property = "")
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}
