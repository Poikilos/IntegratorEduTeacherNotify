/*
 * Created by SharpDevelop.
 * User: jgustafson
 * Date: 10/11/2016
 * Time: 12:00 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ExpertMultimedia
{
	/// <summary>
	/// Description of MessageForm.
	/// </summary>
	public partial class MessageForm : Form
	{
		public static MessageForm msgform = null;
		public MessageForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}
		
		public static void ShowMessage(string msg, string caption) {
			//try {
				if (MessageForm.msgform!=null && !MessageForm.msgform.IsDisposed) MessageForm.msgform.Hide();
				if (MessageForm.msgform!=null && MessageForm.msgform.IsDisposed) MessageForm.msgform=null;
				if (MessageForm.msgform==null) MessageForm.msgform=new MessageForm();
				MessageForm.msgform.Text = caption;
				MessageForm.msgform.mainTextBox.Text = msg;
				MessageForm.msgform.Show();
				MessageForm.msgform.BringToFront();
			//}
			//catch (Exception exn) {
				//MessageBox.Show("Could not finish static ShowMessage: "+exn.ToString(), "IEdu TeacherNotify");
			//}
		}
		
		void MessageFormLoad(object sender, EventArgs e)
		{
			
		}
		
		void Button0Click(object sender, EventArgs e)
		{
			this.Hide();
		}
		
		void MessageFormFormClosed(object sender, FormClosedEventArgs e)
		{
			MessageForm.msgform=null;
		}
	}
}
