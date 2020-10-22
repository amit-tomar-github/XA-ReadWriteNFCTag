using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Nfc;
using Android.Content;
using System.Text;
using System;
using Android.Nfc.Tech;
using System.IO;

namespace ReadNFGTags
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private NfcAdapter _nfcAdapter;
        TextView txtMsg;
        EditText editTagId;
        RadioButton radio_read, radio_write;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            editTagId = FindViewById<EditText>(Resource.Id.editTagId);
            txtMsg = FindViewById<TextView>(Resource.Id.txtMsg);
            txtMsg.Text = "";

            radio_read = FindViewById<RadioButton>(Resource.Id.radio_read);
            radio_write = FindViewById<RadioButton>(Resource.Id.radio_write);
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
        }
        protected override void OnResume()
        {
            try
            {
                base.OnResume();

                if (_nfcAdapter == null)
                {
                    var alert = new Android.App.AlertDialog.Builder(this).Create();
                    alert.SetMessage("NFC is not supported on this device.");
                    alert.SetTitle("NFC Unavailable");
                    alert.Show();
                }
                else
                {
                    // var tagDetected = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
                    var tagDetected = new IntentFilter(NfcAdapter.ActionTagDiscovered);
                    var filters = new[] { tagDetected };

                    var intent = new Intent(this, this.GetType()).AddFlags(ActivityFlags.SingleTop);

                    var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);

                    _nfcAdapter.EnableForegroundDispatch(this, pendingIntent, filters, null);
                }
            }
            catch (Exception ex) { Toast.MakeText(this, ex.Message, ToastLength.Long).Show(); }
        }

        protected override void OnNewIntent(Intent intent)
        {
            try
            {
                txtMsg.Text = "";
                if (intent.Action == NfcAdapter.ActionTagDiscovered)
                {
                    var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
                    if (tag != null)
                    {
                        if (radio_read.Checked)
                        {
                            // First get all the NdefMessage
                            var rawMessages = intent.GetParcelableArrayExtra(NfcAdapter.ExtraNdefMessages);
                            if (rawMessages != null)
                            {
                                var msg = (NdefMessage)rawMessages[0];

                                // Get NdefRecord which contains the actual data
                                var record = msg.GetRecords()[0];
                                if (record != null)
                                {
                                    //if (record.Tnf == NdefRecord.TnfWellKnown) // The data is defined by the Record Type Definition (RTD) specification available from http://members.nfc-forum.org/specs/spec_list/
                                    // {
                                    // Get the transfered data
                                    var data = Encoding.ASCII.GetString(record.GetPayload());
                                    txtMsg.Text = "Tag Data : " + data;
                                    // }
                                }
                            }
                            else
                                txtMsg.Text = "No data in tag";
                        }
                        else //Write Tag
                        {
                            var payload = Encoding.ASCII.GetBytes(editTagId.Text.Trim());
                            var mimeBytes = Encoding.ASCII.GetBytes("text/plain");
                            var record = new NdefRecord(NdefRecord.TnfWellKnown, mimeBytes, new byte[0], payload);
                            var ndefMessage = new NdefMessage(new[] { record });

                            if (!TryAndWriteToTag(tag, ndefMessage))
                            {
                                // Maybe the write couldn't happen because the tag wasn't formatted?
                                TryAndFormatTagWithMessage(tag, ndefMessage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { txtMsg.Text = "Error:" + ex.Message; }
        }
        /// <summary>
        /// This method will try and write the specified message to the provided tag. 
        /// </summary>
        /// <param name="tag">The NFC tag that was detected.</param>
        /// <param name="ndefMessage">An NDEF message to write.</param>
        /// <returns>true if the tag was written to.</returns>
        private bool TryAndWriteToTag(Tag tag, NdefMessage ndefMessage)
        {

            // This object is used to get information about the NFC tag as 
            // well as perform operations on it.
            var ndef = Ndef.Get(tag);
            if (ndef != null)
            {
                ndef.Connect();

                // Once written to, a tag can be marked as read-only - check for this.
                if (!ndef.IsWritable)
                {
                    DisplayMessage("Tag is read-only.");
                }

                // NFC tags can only store a small amount of data, this depends on the type of tag its.
                var size = ndefMessage.ToByteArray().Length;
                if (ndef.MaxSize < size)
                {
                    DisplayMessage("Tag doesn't have enough space.");
                }

                ndef.WriteNdefMessage(ndefMessage);
                DisplayMessage("Succesfully wrote tag.");
                return true;
            }

            return false;
        }
        private bool TryAndFormatTagWithMessage(Tag tag, NdefMessage ndefMessage)
        {
            var format = NdefFormatable.Get(tag);
            if (format == null)
            {
                DisplayMessage("Tag does not appear to support NDEF format.");
            }
            else
            {
                try
                {
                    format.Connect();
                    format.Format(ndefMessage);
                    DisplayMessage("Tag successfully written.");
                    return true;
                }
                catch (IOException ioex)
                {
                    var msg = "There was an error trying to format the tag.";
                    DisplayMessage(msg);
                    //Log.Error(Tag, ioex, msg);
                }
            }
            return false;
        }
        private void DisplayMessage(string message)
        {
            txtMsg.Text = message;
        }
            
        public void WriteToTag(Intent intent, string content)
        {
            try
            {
                if (content != "")
                {
                    var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
                    if (tag != null)
                    {
                        Ndef ndef = Ndef.Get(tag);
                        if (ndef != null && ndef.IsWritable)
                        {
                            var payload = Encoding.ASCII.GetBytes(content);
                            var mimeBytes = Encoding.ASCII.GetBytes("text/plain");
                            var record = new NdefRecord(NdefRecord.TnfWellKnown, mimeBytes, new byte[0], payload);
                            var ndefMessage = new NdefMessage(new[] { record });

                            ndef.Connect();
                            ndef.WriteNdefMessage(ndefMessage);
                            ndef.Close();
                            txtMsg.Text = "Write Successfully!!";
                        }
                    }
                }
                else
                    txtMsg.Text = "Input tag data";
            }
            catch (Exception ex) { txtMsg.Text = "Error:" + ex.Message; }
        }
    }
}