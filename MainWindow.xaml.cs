using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FinalProject_UCLAChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Fields
        {
            public string message { get; set; }
            public string username { get; set; }
        }

        public class RootObject
        {
            public Fields fields { get; set; }
        }

        public class Fields_dm
        {
            public string sender { get; set; }
            public string message { get; set; }

        }
        public class RootObject_dm
        {
            public Fields_dm fields { get; set; }
        }

        List<Grid> allGrids;
        string username = "";
        string receiver = "";
        HttpClient chat_client = new HttpClient();
        List<string> message_list = new List<string>();
        List<string> username_list = new List<string>();
        List<string> search_list = new List<string>();
        List<string> chat_list = new List<string>();
        List<string> block_user_list = new List<string>();
        List<Dictionary<string, List<string>>> block_data = new List<Dictionary<string, List<string>>>();

        public MainWindow()
        {
            InitializeComponent();

            allGrids = new List<Grid>()
            {
                grid_block,
                grid_chat,
                grid_direct_message1,
                grid_direct_message2,
                grid_search,
                grid_sign_in
            };

            load_blocklist_data();

            ShowGrid(grid_sign_in);
            txt_sign_in.Focus();
                    
        }

        #region buttons
        private void Btn_chat_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(grid_chat);
            GetChatMessageAsync(lb_chat);
            txt_chat.Focus();
        }

        private void Btn_direct_message_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(grid_direct_message1);
            txt_dm_user.Focus();
        }

        private void Btn_search_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(grid_search);
            GetChatMessageAsync(lb_search);
        }

        private void Btn_block_Click(object sender, RoutedEventArgs e)
        {
            ShowGrid(grid_block);       
            load_block();
        }

        private void Btn_dm_user_Click(object sender, RoutedEventArgs e)
        {
            GetRecevier();
            ShowGrid(grid_direct_message2);
            GetDirectMessageAsync();
            lbl_dm.Content = receiver;
            txt_direct_message.Focus();
        }

        private void Txt_dm_user_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Btn_dm_user_Click(this, new RoutedEventArgs());
            }
        }

        private void GetRecevier()
        {
            if(string.IsNullOrEmpty(txt_dm_user.Text))
            {
     
                MessageBox.Show("Mate, you are about to send to nobody." ,"",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                receiver = txt_dm_user.Text;             
            }

        }

        #endregion

        #region sign-in
        private void Btn_sign_in_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txt_sign_in.Text))
            {
                MessageBoxResult result = MessageBox.Show("Fancy entering a username, mate? \n" +
                    "(username is set to \"Default\", if choose No)",
                    "",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.No)
                {
                    username = "Default";

                    ShowGrid(grid_chat);
                    GetChatMessageAsync(lb_chat);

                    txt_chat.Focus();
                }

            }
            else
            {
                username = txt_sign_in.Text;

                ShowGrid(grid_chat);
                GetChatMessageAsync(lb_chat);

                txt_chat.Focus();
            }


        }

        private void Txt_sign_in_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Btn_sign_in_Click(this, new RoutedEventArgs());
            }

        }

        #endregion


        #region chat
        
        private void Btn_chat_send_Click(object sender, RoutedEventArgs e)
        {
            PostChatMessageAsync(txt_chat);
            load_chat(lb_chat);
        }

        private void Txt_chat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Btn_chat_send_Click(this, new RoutedEventArgs());
            }
        }
        #endregion


        #region direct_message

        private void Btn_direct_message_send_Click(object sender, RoutedEventArgs e)
        {
            GetDirectMessageAsync();
            PostDirectMessageAsync();
            GetDirectMessageAsync();
        }

        private void Txt_direct_message_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Btn_direct_message_send_Click(this, new RoutedEventArgs());
            }
        }

        private async void PostDirectMessageAsync()
        {
            string direct_message = txt_direct_message.Text;

            var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("sender", username),
                    new KeyValuePair<string, string>("message", direct_message),
                    new KeyValuePair<string, string>("receiver", receiver),

                });

            string url = "https://chat-ucla.herokuapp.com/direct_message";

            await chat_client.PostAsync(url, content);

            txt_direct_message.Clear();
            txt_direct_message.Focus();

        }

        private async void GetDirectMessageAsync()
        {
            HttpResponseMessage response = await chat_client.GetAsync("https://chat-ucla.herokuapp.com/direct_message?sender=" + username + "&receiver=" + receiver);

            var source = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<List<RootObject_dm>>(source);

            var dm_list = json.Select(x => x.fields.sender + ": " + x.fields.message);
            lb_direct_message.Items.Clear();
            foreach (var item in dm_list)
            {
                lb_direct_message.Items.Add(item);
            }
        }

       
        #endregion

        #region search
        private void Btn_search_send_Click(object sender, RoutedEventArgs e)
        {
            GetSearchResultAsync();
        }

        

        private void Txt_search_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                GetSearchResultAsync();
            }
        }

        private async void GetSearchResultAsync()
        {
            string keyword = txt_search.Text;

            HttpResponseMessage response = await chat_client.GetAsync("https://chat-ucla.herokuapp.com/chats" + "?search=" + keyword);

            var source = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<List<RootObject>>(source);
            message_list = json.Select(x => x.fields.message).ToList();
            username_list = json.Select(x => x.fields.username).ToList();
            search_list = json.Where(x => x.fields.message.Contains(keyword)).Select(x => x.fields.username + ": " + x.fields.message).ToList();

            load_search();

            txt_search.Clear();
            txt_search.Focus();
        }
        #endregion

        #region block

        private void Btn_block_send_Click(object sender, RoutedEventArgs e)
        {
            string block_user = txt_block.Text;
            block_user_list.Add(block_user);

            load_block();

            txt_block.Clear();
            txt_block.Focus();
        }

        private void Txt_block_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Btn_block_send_Click(this, new RoutedEventArgs());
            }
        }

        private void Btn_remove_block_Click(object sender, RoutedEventArgs e)
        {

            if (lb_block.SelectedIndex > -1)
            {
                int index = lb_block.SelectedIndex;
                string blockedUser = block_user_list[index];

                block_user_list.Remove(blockedUser);

                load_block();
            }


        }

        #endregion

        #region functions

        public void ShowGrid(Grid grid)
        {
            foreach (var g in allGrids)
            {
                if (g.Name.Equals(grid.Name))
                {
                    g.Visibility = Visibility.Visible;
                }
                else
                {
                    g.Visibility = Visibility.Hidden;
                }
            }
        }

        //POST message to server and display in lb_chat
        public async void PostChatMessageAsync(TextBox tb)
        {
           
            var message = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("message", tb.Text)
                });

            HttpResponseMessage response = await chat_client.PostAsync("https://chat-ucla.herokuapp.com/chats", message);
            GetChatMessageAsync(lb_chat);
            tb.Text = string.Empty;
            tb.Focus();
        }


        //GET chat
        private async void GetChatMessageAsync(ListBox lb)
        {
            int num_messages = 50;

            HttpResponseMessage chat_response = await chat_client.GetAsync("https://chat-ucla.herokuapp.com/chats?num_messages=" + num_messages);

            var chat_source = await chat_response.Content.ReadAsStringAsync();
            var chat_json = JsonConvert.DeserializeObject<List<RootObject>>(chat_source);
            message_list = chat_json.Select(x => x.fields.message).ToList();
            username_list = chat_json.Select(x => x.fields.username).ToList();

            if (block_user_list.Count > 0)
            {
                List<int> index_list = new List<int>();
            
                foreach(var b_user in block_user_list)
                {
                    for(int i = 0; i < username_list.Count; i ++)
                    {
                        if(b_user.Equals(username_list[i]))
                        {
                            index_list.Add(i);
                        }
                    }
                }


                for(int i = 0; i < message_list.Count; i ++)
                {
                    if(index_list.Contains(i))
                    {
                        message_list[i] = "*** blocked ***";
                    }
                }               
            }

            chat_list = username_list.Zip(message_list, (a, b) => (a + ": " + b)).ToList();

            load_chat(lb);
        }

        private void load_chat(ListBox lb)
        {
            lb.Items.Clear();
            foreach(var message in chat_list)
            {
                lb.Items.Add(message);
            }
        }

        private void load_block()
        {
            lb_block.Items.Clear();
            foreach (var username in block_user_list)
            {
                lb_block.Items.Add(username);
            }
        }

        private void load_search()
        {
            lb_search.Items.Clear();
            foreach (var item in search_list)
            {
                lb_search.Items.Add(item);
            }
        }

        private void load_blocklist_data()
        {
            if(File.Exists(@"block.txt"))
            {
                var text_data = File.ReadAllText(@"block.txt");
                block_data = JsonConvert.DeserializeObject<List<Dictionary<string, List<string>>>>(text_data);

                foreach (var dict in block_data)
                {
                    foreach (var pair in dict)
                    {
                        if (username == pair.Key)
                        {
                            block_user_list = pair.Value;
                            break;
                        }
                    } 
                }
          
            }
        }

        #endregion


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var current_block = new Dictionary<string, List<string>>()
            {
                { username, block_user_list}
            };

            block_data.Add(current_block);

            string serializedJson = JsonConvert.SerializeObject(block_data);

            File.WriteAllText(@"block.txt", serializedJson);
            
        }

        
    }
}
