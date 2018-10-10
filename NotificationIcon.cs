/*
 * Created by SharpDevelop.
 * User: administrator
 * Date: 9/24/2015
 * Time: 9:12 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using Microsoft.VisualBasic.FileIO; // TextFieldParser (requires VisualBasic.dll

namespace ExpertMultimedia
{
	public sealed class NotificationIcon
	{
		private NotifyIcon notifyIcon;
		private ContextMenu notificationMenu;
		public static bool is_verbose=false;
		private static string data_path = null;
		private static string sounds_path = null;
		private static string events_file_path = null;
		private static string organization_settings_file_path = null;
		private static string organization_current_calendar_path = null;
		private static string organization_date_ranges_file_path = null;
		private static string schedule_path=null;
		private static string status_string = "";
		private static YAMLObject daterangesYO = null;
		private static string data_path_specifier_file_name="data_path.txt";
		private static string locally_ignored_categories_file_name="ignored_categories.txt";
		private static ArrayList locally_ignored_categories_list=null;
		private static readonly string my_display_name = "IntegratorEdu TeacherNotify";
		private static readonly string my_display_name_and_version = "IntegratorEdu TeacherNotify (r20161011)";
		private static readonly string my_about_string = "This program executes important organization-scheduled alerts which may include messages or periodic sounds.";
		private static int secondly_check_count = 0;
		private static int thirty_minute_check_count = 0;
		private static System.Windows.Forms.Timer secondlyTimer = null;
		private static System.Windows.Forms.Timer thirtyMinuteTimer = null;
		private static FastTable daily_disposable_events_table = null;
		//private static RTable daily_disposable_events_table = null;
		private static DateTime last_daily_alert_not_including_before_loadtime_alerts_time = DateTime.MinValue; //should be loaded from file later for non-disposable events
		private static DateTime settings_load_datetime = DateTime.Now;
		private static DateTime program_load_datetime = DateTime.Now;
		private static string categories_path = null;
		private static string current_schedule_name = null;
		private static string last_error_string = "";
		private static string last_alert_string = "";
		private static string organization_current_calendar_name=null;
		public static bool any_disposable_timed_events_today = false;
		public static bool is_secondly_tick_event = false;
		
		public ArrayList recurring_event_categories = new ArrayList();
		System.Media.SoundPlayer soundplayer = new System.Media.SoundPlayer();
		#region Initialize icon and menu
		public NotificationIcon()
		{
			notifyIcon = new NotifyIcon();
			notificationMenu = new ContextMenu(InitializeMenu());
			
			notifyIcon.DoubleClick += iconDoubleClick;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationIcon));
			notifyIcon.Icon = (Icon)resources.GetObject("$this.Icon");
			notifyIcon.ContextMenu = notificationMenu;

			thirtyMinuteTimer = new System.Windows.Forms.Timer();
			thirtyMinuteTimer.Interval = 1000*60*10;
			thirtyMinuteTimer.Tick += new EventHandler(thirtyMinuteTimerTick);
			
			secondlyTimer = new System.Windows.Forms.Timer();
			secondlyTimer.Interval = 1000;
			secondlyTimer.Tick += new EventHandler(secondlyTimerTick);
			last_daily_alert_not_including_before_loadtime_alerts_time = settings_load_datetime;
			loadSettings();
			thirtyMinuteTimer.Start();
			secondlyTimer.Start();
			MessageForm.msgform = new MessageForm();
		}
		private void loadSettings() {
			
			if (data_path==null) {
				//only use hard drive once per run:
				string data_path_specifier_file_fullname = Path.Combine(@"C:\ProgramData\IntegratorEduTeacherNotify", data_path_specifier_file_name);
				string locally_ignored_categories_file_fullname = Path.Combine(@"C:\ProgramData\IntegratorEduTeacherNotify", locally_ignored_categories_file_name);
				try {
					if (File.Exists(data_path_specifier_file_fullname)) {
						StreamReader streamIn = new StreamReader(data_path_specifier_file_fullname);
						string line;
						string line_strip;
						while ( (line=streamIn.ReadLine()) != null ) {
							line_strip = line.Trim();
							if (line_strip.Length>0) {
								data_path=line_strip;
							}
						}
						streamIn.Close();
					}
					if (File.Exists(locally_ignored_categories_file_fullname)) {
						StreamReader streamIn = new StreamReader(locally_ignored_categories_file_fullname);
						string line;
						string line_strip;
						while ( (line=streamIn.ReadLine()) != null ) {
							line_strip = line.Trim();
							if (line_strip.Length>0) {
								if (locally_ignored_categories_list==null) locally_ignored_categories_list=new ArrayList();
								locally_ignored_categories_list.Add(line_strip);
							}
						}
						streamIn.Close();
					}
					if (string.IsNullOrEmpty(data_path) || !Directory.Exists(data_path)) {
						//data_path=@"..\IntegratorEduTeacherNotify_FCA_data"
						Console.Error.WriteLine("Missing schedule data");
					}
				}
				catch (Exception exn) {
					Console.Error.WriteLine("loadSettings could not finish: "+exn.ToString());
				}
			}
			if (data_path==null) data_path=@"\\FCAFILES\main\Operations\BellSettings";;
			sounds_path = Path.Combine(data_path, "sounds");
			organization_settings_file_path = Path.Combine(data_path, "settings.yml");
			YAMLObject settingsYO=new YAMLObject();
			
			is_verbose=true; //debug only
			YAMLObject.is_verbose=is_verbose;
			if (is_verbose) Console.Error.WriteLine();
			if (is_verbose) Console.Error.WriteLine(organization_settings_file_path+"...");
			
			settingsYO.load(organization_settings_file_path);
			ArrayList error_strings=settingsYO.deq_errors_in_yaml_syntax();
			if (error_strings!=null&&error_strings.Count>0) {
				Console.Error.WriteLine(error_strings.Count.ToString()+" syntax error(s) in "+organization_settings_file_path+":");
				foreach (string error_string in error_strings) {
					Console.Error.WriteLine(error_string);
				}
			}
			
			organization_current_calendar_name = settingsYO.get_sub_value("current_calendar");
			organization_current_calendar_path = Path.Combine(data_path, organization_current_calendar_name);
			Console.Error.WriteLine("organization_current_calendar_path:"+organization_current_calendar_path);
			organization_date_ranges_file_path = Path.Combine(organization_current_calendar_path, "date ranges.yml");
			daterangesYO = new YAMLObject();
			if (is_verbose) Console.Error.WriteLine();
			if (is_verbose) Console.Error.WriteLine(organization_date_ranges_file_path+"...");
			
			daterangesYO.load(organization_date_ranges_file_path);
			error_strings=daterangesYO.deq_errors_in_yaml_syntax();
			if (error_strings!=null&&error_strings.Count>0) {
				Console.Error.WriteLine(error_strings.Count.ToString()+" syntax error(s) in "+organization_date_ranges_file_path+":");
				foreach (string error_string in error_strings) {
					Console.Error.WriteLine(error_string);
				}
			}
			
			if (is_verbose) {
				Console.Error.WriteLine("organization_date_ranges.dump_to_stderr():");
				daterangesYO.dump_to_stderr();
				daterangesYO.save("output.verbose.dateranges.yml");
			}
			int index=0;
			bool is_active=false;
			schedule_path=null; //the current schedule where today is in its range
			current_schedule_name=null;
			while (!is_active) {
				YAMLObject daterangeYO = daterangesYO.get_array_value(index);
				if (daterangeYO!=null) {
					current_schedule_name=daterangeYO.get_sub_value("schedule");
					string daterange_first=daterangeYO.get_sub_value("first");
					string daterange_last=daterangeYO.get_sub_value("last");
					Console.Error.WriteLine("["+index.ToString()+"]:");
					if (current_schedule_name!=null) {
						Console.Error.WriteLine("  schedule:"+current_schedule_name);
						if (daterange_first!=null) {
							Console.Error.WriteLine("  first:"+daterange_first);
							if (daterange_last!=null) {
								Console.Error.WriteLine("  last:"+daterange_last);
								DateTime start_dt=DateTime.Parse(daterange_first+"/"+DateTime.Now.Year.ToString()+" 0:00");
								DateTime end_dt=DateTime.Parse(daterange_last+"/"+DateTime.Now.Year.ToString()+" 23:59");
								if (DateTime.Now>=start_dt && DateTime.Now<=end_dt) {
									Console.Error.WriteLine("Current schedule:"+current_schedule_name);
									schedule_path = Path.Combine(organization_current_calendar_path, current_schedule_name);
								}
								else {
									Console.Error.WriteLine( DateTime.Now.ToString("yyy-MM-dd HH:mm:ss")
									                        + " is not in date range "
									                        + start_dt.ToString("yyy-MM-dd HH:mm:ss")
									                        + " to "
									                        + end_dt.ToString("yyy-MM-dd HH:mm:ss")
									                       );
								}
							}
							else Console.Error.WriteLine("  last:null");
						}
						else Console.Error.WriteLine("  first:null");
					}
					else Console.Error.WriteLine("  schedule:null");
				}
				else {
					break;
				}
				index++;
			}
			recurring_event_categories.Clear();
			if (schedule_path!=null) {
				events_file_path = Path.Combine(schedule_path, "Events, Daily Disposable.csv");
				daily_disposable_events_table = new RTable();
				Console.Error.WriteLine("Setting schedule to '"+events_file_path+"' since today is in it's daterange.");
				daily_disposable_events_table.Load(events_file_path, true);
				if (daily_disposable_events_table
			}
			else {
				daily_disposable_events_table=null;
			}
			categories_path=Path.Combine(data_path, "RecurringEventCategories");
			try {
				DirectoryInfo recDI=new DirectoryInfo(categories_path);
				foreach (FileInfo thisFI in recDI.GetFiles()) {
					if (!thisFI.Name.StartsWith(".") && thisFI.Name.ToLower().EndsWith(".yml")) {
						IEduEventCategory thisEC=new IEduEventCategory();
						thisEC.name=thisFI.Name.Substring(0,thisFI.Name.Length-4);
						YAMLObject thisYO=new YAMLObject();
						if (is_verbose) Console.Error.WriteLine();
						if (is_verbose) Console.Error.WriteLine(thisFI.FullName+"...");
						thisYO.load(thisFI.FullName);
						ArrayList cat_error_strings=thisYO.deq_errors_in_yaml_syntax();
						if (cat_error_strings!=null&&cat_error_strings.Count>0) {
							Console.Error.WriteLine(cat_error_strings.Count.ToString()+" syntax error(s) in "+thisFI.FullName+":");
							foreach (string error_string in cat_error_strings) {
								Console.Error.WriteLine(error_string);
							}
						}
						thisEC.sound_file_path=thisYO.get_sub_value("sound");
						thisEC.message=thisYO.get_sub_value("message");
						thisEC.message_heading=thisYO.get_sub_value("message_heading");
						recurring_event_categories.Add(thisEC);
						//Console.Error.WriteLine("sound:"+((thisEC.sound_file_path!=null)?thisEC.sound_file_path:"null"));
					}
				}
			}
			catch (Exception exn) {
				last_error_string=exn.ToString();
			}

			settings_load_datetime = DateTime.Now;
			last_daily_alert_not_including_before_loadtime_alerts_time = settings_load_datetime; //prevents loading backlogged events
			updateStatus();
		}
		private void thirtyMinuteTimerTick(object sender, EventArgs e) {
			thirty_minute_check_count+=1;
			loadSettings();
			updateStatus();
		}
		private string getDayFromTwoChars(string two_char_day_string) {
			string result = null;
			if (two_char_day_string!=null) {
				string two_char_day_string_lower = two_char_day_string.ToLower();
				if (two_char_day_string_lower == "mo") result="Monday";
				else if (two_char_day_string_lower == "tu") result="Tuesday";
				else if (two_char_day_string_lower == "we") result="Wednesday";
				else if (two_char_day_string_lower == "th") result="Thursday";
				else if (two_char_day_string_lower == "fr") result="Friday";
				else if (two_char_day_string_lower == "sa") result="Saturday";
				else if (two_char_day_string_lower == "su") result="Sunday";
				else last_error_string="There is an unrecognized day abbreviation '"+two_char_day_string+"' in "+events_file_path+" (must be first two letters or whole name of day)";
			}
			else last_error_string="A day abbreviation is null in "+events_file_path;
			return result;
		}
		private void secondlyTimerTick(object sender, EventArgs e) {
			string my_error_string="";
			if (secondlyTimer.Enabled) {
				any_disposable_timed_events_today = false;
				secondlyTimer.Enabled=false;
				secondly_check_count++;
				if (daily_disposable_events_table != null) {
					try {
						my_error_string="";
						int Time_Column = daily_disposable_events_table.InternalColumnIndexOfI_AssumingNeedleIsLower("time");
						if (Time_Column<0) my_error_string+="missing Time column ";
						int Category_Column = daily_disposable_events_table.InternalColumnIndexOfI_AssumingNeedleIsLower("category");
						if (Category_Column<0) my_error_string+="missing Category column ";
						int Description_Column = daily_disposable_events_table.InternalColumnIndexOfI_AssumingNeedleIsLower("description");
						if (Description_Column<0) my_error_string+="missing Description column ";
						int Days_Column = daily_disposable_events_table.InternalColumnIndexOfI_AssumingNeedleIsLower("days");
						if (Days_Column<0) my_error_string+="missing Days column ";
						if (string.IsNullOrEmpty(my_error_string)) {
							DateTime temporary_last_daily_alert_time = DateTime.MinValue;
							for (int row_index=0; row_index<daily_disposable_events_table.Rows; row_index++) {
								
								string days_string=daily_disposable_events_table.GetForcedString(row_index, Days_Column);
								if (!string.IsNullOrEmpty(days_string)) {
									string[] days_array=days_string.Split(';');
									bool this_event_fires_today = false;
									for (int relative_day_entry_index=0; relative_day_entry_index<days_array.Length; relative_day_entry_index++) {
										if ( (days_array[relative_day_entry_index].Length==2 && DateTime.Now.ToString("dddd").ToLower().Substring(0,2) == days_array[relative_day_entry_index].ToLower())
										    ||  DateTime.Now.ToString("dddd").ToLower() == days_array[relative_day_entry_index].ToLower()
										    ||  DateTime.Now.ToString("ddd").ToLower() == days_array[relative_day_entry_index].ToLower()
										   ) {
											this_event_fires_today=true;
											break;
										}
										else {
											//Console.Error.WriteLine(days_array[relative_day_entry_index].ToLower()+" is not today ("+DateTime.Now.ToString("dddd").ToLower()+")");
										}
									}
									if (this_event_fires_today) {
										any_disposable_timed_events_today=true;
										string category_string=daily_disposable_events_table.GetForcedString(row_index, Category_Column);
										string description_string=daily_disposable_events_table.GetForcedString(row_index, Description_Column);
										string time_string=daily_disposable_events_table.GetForcedString(row_index, Time_Column);
										DateTime thisDateTime = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd ")+time_string);
										if ( thisDateTime>=program_load_datetime
											//thisDateTime<=DateTime.Now
										    //&& thisDateTime>last_daily_alert_not_including_before_loadtime_alerts_time
										    && DateTime.Now.ToString("yyyy-MM-dd HH:mm")!=last_daily_alert_not_including_before_loadtime_alerts_time.ToString("yyyy-MM-dd HH:mm")
										    && DateTime.Now.ToString("yyyy-MM-dd HH:mm")==thisDateTime.ToString("yyyy-MM-dd HH:mm")
										   ) {
											if (is_verbose) {
												Console.Error.WriteLine();
												Console.Error.WriteLine("Event instance DateTime:"+thisDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
											}
											last_alert_string="(invalid category '"+category_string+"')";
											string category_path=Path.Combine(categories_path,category_string+".yml");
											bool is_good_ec=false;
											string categories_to_string="";
											bool is_ignored=false;
											string category_string_lower = category_string.ToLower();
											if (locally_ignored_categories_list!=null) {
												foreach (string ignore_ec_string in locally_ignored_categories_list) {
													if (ignore_ec_string.ToLower()==category_string_lower) {
														is_ignored=true;
													}
												}
											}
											//TODO: make this case insensitive:
											if (!is_ignored) {
												foreach (IEduEventCategory check_ec in recurring_event_categories) {
													if (check_ec!=null) {
														if (check_ec.name.ToLower()==category_string.ToLower()) {
															categories_to_string+=(string.IsNullOrEmpty(categories_to_string))?check_ec.name:categories_to_string+", "+check_ec.name;
															is_good_ec=true;
															last_alert_string="";
															if (!string.IsNullOrEmpty(check_ec.sound_file_path)) {
																soundplayer.SoundLocation=Path.Combine(data_path,check_ec.sound_file_path);
																string alert_part_string=" Play(@\""+soundplayer.SoundLocation.Replace("\"","\\\"")+"\")";
																last_alert_string+=alert_part_string;
																Console.Error.Write(alert_part_string);
																soundplayer.Play();
															}
															if (!string.IsNullOrEmpty(check_ec.message_heading) || !string.IsNullOrEmpty(check_ec.message)) {
																string msg=(check_ec.message!=null)?check_ec.message:"";
																string caption=(check_ec.message_heading!=null?check_ec.message_heading:"");
																if (string.IsNullOrEmpty(caption)) caption=my_display_name_and_version;
																//else caption+=" via "+my_display_name;
																last_alert_string+="; MessageForm (\""+msg.Replace("\"","\\\"")+"\", \""+caption.Replace("\"","\\\"")+"\")";
																
																MessageForm.ShowMessage(msg, caption);
															}
															last_alert_string+=" ("+thisDateTime.ToString("yyyy-MM-dd HH:mm")+")";
														}
													}
												}
												if (!is_good_ec) {
													last_alert_string="(invalid category '"+category_string+"': not found among list [ "+categories_to_string+"])";
												}
											}
											else last_alert_string="";  // in locally_ignored_categories_list
											
											temporary_last_daily_alert_time = DateTime.Now; //Since using temporary value, will not prevent other event of same second but different category from firing?
										}//end if in range of last time and this time, and now is not same minute as last check, but event is same minute as now
										else {
											if (DateTime.Now.ToString("yyyy-MM-dd HH:mm")==thisDateTime.ToString("yyyy-MM-dd HH:mm")) {
												Console.Error.WriteLine("  ignoring event since:");
												Console.Error.WriteLine("    DateTime.Now: "+DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
												Console.Error.WriteLine("    event: "+DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
												Console.Error.WriteLine("    last alert: "+last_daily_alert_not_including_before_loadtime_alerts_time.ToString("yyyy-MM-dd HH:mm"));
											}
										}
									}
								}
								else {
									my_error_string=events_file_path+" is missing Days for alert on line "+(row_index+2).ToString()+" so it will never happen."; //+2 to convert to counting number AND add title row
								}
							}
							if (temporary_last_daily_alert_time!=DateTime.MinValue) {
								//for purpose of this, see comment on previous usage of temporary_last_daily_alert_time
								last_daily_alert_not_including_before_loadtime_alerts_time = temporary_last_daily_alert_time;
							}
						}
					}
					catch (Exception exn){
						if (!string.IsNullOrEmpty(my_error_string)) my_error_string=my_error_string+" AND ";
						my_error_string+="secondlyTimerTick could not finish because "+exn.ToString();
					}
				}
				else {
					my_error_string="secondlyTimerTick found null daily_disposable_events_table.";
				}
				secondlyTimer.Enabled=true;
			}
			else {
				my_error_string="secondly timer was already firing.";
			}
			if (!string.IsNullOrEmpty(my_error_string)) last_error_string=my_error_string;
			updateStatus();
		}//end secondlyTimerTick
		private void updateStatus() {
			status_string = "\n\nCurrent status:";
			status_string+="\n  secondly check count: "+secondly_check_count.ToString();
			status_string+="\n  thirty minute check count: "+thirty_minute_check_count.ToString();
			status_string+="\n  any timed events today: "+(any_disposable_timed_events_today?"yes":"no");
			status_string+="\n  current calendar: "+(organization_current_calendar_name!=null?organization_current_calendar_name:"(organization has no calendar)");
			status_string+="\n  current schedule name: "+(current_schedule_name!=null?current_schedule_name:"(no schedule for today's date)");
			if (last_daily_alert_not_including_before_loadtime_alerts_time!=settings_load_datetime) {
				status_string+="\n last alert time:"+last_daily_alert_not_including_before_loadtime_alerts_time.ToString("yyyy-MM-dd HH:mm:ss");
			}
			if (!string.IsNullOrEmpty(last_alert_string)) {
				status_string+="\n last alert description:"+last_alert_string;
			}
			if (!string.IsNullOrEmpty(last_error_string)) {
				status_string+="\n  last error: "+last_error_string.ToString();
			}
		}
		private MenuItem[] InitializeMenu()
		{
			MenuItem[] menu = new MenuItem[] {
				new MenuItem("About", menuAboutClick),
				new MenuItem("Exit", menuExitClick)
			};
			return menu;
		}
		#endregion
		
		#region Main - Program entry point
		/// <summary>Program entry point.</summary>
		/// <param name="args">Command Line Arguments</param>
		[STAThread]
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			bool isFirstInstance;
			// Please use a unique name for the mutex to prevent conflicts with other programs
			using (Mutex mtx = new Mutex(true, "IntegratorEduTeacherNotify", out isFirstInstance)) {
				if (isFirstInstance) {
					NotificationIcon notificationIcon = new NotificationIcon();
					notificationIcon.notifyIcon.Visible = true;
					Application.Run();
					notificationIcon.notifyIcon.Dispose();
				} else {
					MessageForm.ShowMessage("There is more than one user logged in. Please log off, go to the other then user off that user too, then log back in (until then, your board calibration and notifications may not work properly).",my_display_name_and_version);
					// The application is already running
					// TODO: Display message box or change focus to existing application instance
				}
			} // releases the Mutex
		}
		#endregion
		
		#region Event Handlers
		private void menuAboutClick(object sender, EventArgs e)
		{
			MessageForm.ShowMessage(my_about_string+status_string, "About "+my_display_name_and_version);
		}
		
		private void menuExitClick(object sender, EventArgs e)
		{
			Application.Exit();
		}
		
		private void iconDoubleClick(object sender, EventArgs e)
		{
			MessageForm.ShowMessage(my_about_string+status_string, "About "+my_display_name_and_version);
		}
		#endregion
	}
}
