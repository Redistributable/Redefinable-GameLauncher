﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Redefinable;
using Redefinable.IO;

using Redefinable.Applications.Launcher;
using Redefinable.Applications.Launcher.Controls.Design;
using Redefinable.Applications.Launcher.Forms;
using Redefinable.Applications.Launcher.Informations;


namespace Redefinable.Applications.Launcher.Core
{
    public static class LauncherStarter
    {
        private static GameGenreCollection genreFullInfo = null;
        private static GameControllerCollection controllerFullInfo = null;

        private static LauncherSettings _initializeSettings()
        {
            // 基本設定
            LauncherSettings settings;

            try
            {
                // 読み込んでみる
                settings = LauncherSettings.Load();
            }
            catch
            {
                // 壊れてるので初期設定を利用
                settings = new LauncherSettings();
                settings.Save();

                // 再度読み込み
                settings = LauncherSettings.Load();
            }

            return settings;
        }

        private static GameCollection _getTargetGames(LauncherSettings settings)
        {
            // ジャンル情報
            DebugConsole.Push("COR", "Genre> GenreFilesディレクトリの検索を開始します。");
            DebugConsole.Push("COR", "Genre> " + settings.GenreFilesDirectory);

            if (!Directory.Exists(settings.GenreFilesDirectory))
                throw new DirectoryNotFoundException("GenreFilesDirectoryが見つかりません。 " + settings.GenreFilesDirectory);

            GameGenreCollection genreFullInformations = new GameGenreCollection();
            genreFullInfo = genreFullInformations; 
            genreFullInformations.AddFromDirectory(settings.GenreFilesDirectory);

            if (genreFullInformations.Count == 0)
            {
                genreFullInformations = GameGenreCollection.GetDefaultGenres();
                genreFullInformations.SaveToDirectory(settings.GenreFilesDirectory);
            }


            // コントローラ情報
            DebugConsole.Push("COR", "Cntrl> ControllerFilesディレクトリの検索を開始します。");
            DebugConsole.Push("COR", "Cntrl> " + settings.GenreFilesDirectory);

            if (!Directory.Exists(settings.ControllersFilesDirectory))
                throw new DirectoryNotFoundException("ControllerFilesDirectoryが見つかりません。 " + settings.GenreFilesDirectory);

            GameControllerCollection controllerFullInformations = new GameControllerCollection();
            controllerFullInfo = controllerFullInformations;
            controllerFullInformations.AddFromDirectory(settings.ControllersFilesDirectory);

            if (controllerFullInformations.Count == 0)
            {
                controllerFullInformations = GameControllerCollection.GetDefaultControllers();
                controllerFullInformations.SaveToDirectory(settings.ControllersFilesDirectory);
            }


            // GameFilesディレクトリ
            DebugConsole.Push("COR", "Load> GameFilesディレクトリの検索を開始します。");
            DebugConsole.Push("COR", "Load> " + settings.GameFilesDirectory);

            if (!Directory.Exists(settings.GameFilesDirectory))
                throw new DirectoryNotFoundException("GameFilesDirectoryが見つかりません。 " + settings.GameFilesDirectory);
            
            var gfDir = new GameFilesDirectory(settings.GameFilesDirectory, genreFullInformations, controllerFullInformations);
            var gDirs = gfDir.GetValidDirectories();
            DebugConsole.Push("COR", "Load> 有効なディレクトリは、" + gDirs.Count + "個です。");
            
            if (gDirs.Count == 0 && settings.UseAutoInitializer)
            {
                DebugConsole.Push("COR", "Load> UseAutoInitializerがtrueです。");
                gfDir.InitializeAllDirectory();
                gDirs = gfDir.GetValidDirectories();
                DebugConsole.Push("COR", "Load> 有効なディレクトリは、" + gDirs.Count + "個です。");
            }

            if (settings.ShowPathErrorGame)
                DebugConsole.Push("COR", "Load> 参照エラーが発生している作品も表示する設定になっています。");
            else
                DebugConsole.Push("COR", "Load> 参照エラーが発生しているゲームを検索します。");

            GameCollection games = new GameCollection();
            GameCollection warnings = new GameCollection();
            foreach (var dir in gDirs)
            {
                Game game = dir.Load();
                DebugConsole.Push("COR", "Load> ロード: " + game.Title);

                string fpath = game.GetGameExeFullPath();
                if (File.Exists(fpath))
                {
                    // 本体ファイルが存在する
                    games.Add(game);
                }
                else
                {
                    DebugConsole.Push("COR", "Load> 参照エラー: " + game.Title);
                    DebugConsole.Push("COR", "Load> " + fpath);

                    if (settings.ShowPathErrorGame)
                    {
                        // 本体ファイルが存在しないが、存在しない作品も表示する設定になっている
                        games.Add(game);
                        DebugConsole.Push("COR", "Load> 参照エラーも表示する設定になっています。");
                    }
                    else
                    {
                        DebugConsole.Push("COR", "Load> この作品は非表示になります。");
                    }
                    
                    if (settings.WarningPathError)
                        // 本体ファイルが存在しない場合に、警告する設定になっている
                        warnings.Add(game);
                }
            }

            if (warnings.Count != 0)
            {
                DebugConsole.Push("COR", "Load> 参照エラーの警告が有効になっています。");

                string message = "";
                foreach (var item in warnings)
                    message += "・" + item.Title + "\n";
                MessageBox.Show("次の作品は、ゲーム本体の実行ファイルの参照が正しく設定されていません。\n\n" + message, settings.WindowText, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            return games;
        }
        
        private static LauncherTheme _getLauncherTheme(LauncherSettings settings)
        {
            if (settings.ThemeDirectory[settings.ThemeDirectory.Length - 1] == '\\')
                settings.ThemeDirectory = settings.ThemeDirectory.Substring(0, settings.ThemeDirectory.Length - 1);

            DebugConsole.Push("COR", "Theme> Themesディレクトリの検索を開始します。");
            DebugConsole.Push("COR", "Theme> " + settings.ThemeDirectory);

            string[] files = Directory.GetFiles(settings.ThemeDirectory, "*.rlt", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileName(files[i]);

            if (files.Length == 0)
            {
                // 検出なし
                DebugConsole.Push("COR", "Theme> デフォルトテーマを初期化します。");
                LauncherTheme def = LauncherTheme.GetDefaultTheme();
                def.Save(settings.ThemeDirectory + "\\" + settings.ThemeFile);
            }
            else
                DebugConsole.Push("COR", "Theme> " + String.Join(", ", files));

            return LauncherTheme.Load(settings.ThemeDirectory + "\\" + settings.ThemeFile);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="form"></param>
        private static void _formInitialize(MainForm form, ICollection<Game> games)
        {
            var enm = games.GetEnumerator();
            enm.MoveNext();
            var currentGame = enm.Current;


            // ジャンル情報一覧の追加
            Dictionary<Controls.ChildSelectPanelItem, GameGenre> genreDict = new Dictionary<Controls.ChildSelectPanelItem, GameGenre>();
            foreach (GameGenre gg in genreFullInfo)
            {
                Controls.ChildSelectPanelItem item = new Controls.ChildSelectPanelItem(gg.Name, true);
                form.GetLauncherPanel().GenreSelectPanel.Items.Add(item);
                genreDict.Add(item, gg);
            }

            // コントローラ情報一覧の追加
            Dictionary<Controls.ChildSelectPanelItem, GameController> controllerDict = new Dictionary<Controls.ChildSelectPanelItem, GameController>();
            foreach (GameController gc in controllerFullInfo)
            {
                Controls.ChildSelectPanelItem item = new Controls.ChildSelectPanelItem(gc.Name, true);
                form.GetLauncherPanel().ControllerSelectPanel.Items.Add(item);
                controllerDict.Add(item, gc);
            }

            Func<ICollection<GameGenre>> getCheckedGenres = () =>
            {
                List<GameGenre> result = new List<GameGenre>();
                foreach (var item in form.GetLauncherPanel().GenreSelectPanel.Items)
                    if (item.Checked)
                        result.Add(genreDict[item]);
                return result;
            };

            Func<ICollection<GameController>> getCheckedControllers = () =>
            {
                List<GameController> result = new List<GameController>();
                foreach (var item in form.GetLauncherPanel().ControllerSelectPanel.Items)
                    if (item.Checked)
                        result.Add(controllerDict[item]);
                return result;
            };

            Action viewDetails = () =>
            {
                // currentGameについて表示
                Controls.TitleBar tb = form.GetLauncherPanel().TitleBar;
                tb.RefreshFields(currentGame.DisplayNumber.FullNumber, currentGame.Title);
                Controls.DescriptionPanel dp = form.GetLauncherPanel().DescriptionPanel;
                dp.Message = currentGame.Description;
                
            };

            Action refreshGameList = () =>
            {
                Controls.GameBannerListView listview = form.GetLauncherPanel().GameBannerListView;
                
                listview.SuspendRefreshItem();
                listview.Items.Clear();
                var chkdGenres = getCheckedGenres();
                var chkdControllers = getCheckedControllers();
                foreach (Game g in games)
                {
                    // g = GameFilesから読み込んだうちの１つ

                    foreach(GameGenre gg in chkdGenres)
                        // 現在のチェック済みジャンル一覧の中に、含まれるものがあれば表示
                        if (g.Genres.ContainsGuid(gg))
                        {
                            Controls.GameBanner b = new Controls.GameBanner();
                            b.Text = g.Title;
                            b.Click += (sender, e) => { currentGame = g; viewDetails(); };
                            listview.Items.Add(b);
                        }
                }
                
                listview.ResumeRefreshItem();
            };

            form.GetLauncherPanel().GenreSelectPanel.ChildPanelClosed += (sender, e) =>
            {
                refreshGameList();
            };

            refreshGameList();
            
        }

        /// <summary>
        /// ランチャーを開始します。
        /// </summary>
        public static void Start()
        {
            DebugConsole.Push("Core::Start()");
            DebugConsole.Push("COR", "ソフトウェアを開始します。");

            // 基本設定
            DebugConsole.Push("COR", "基本設定をロードします。");
            LauncherSettings settings = _initializeSettings();

            // 対象作品一覧のロード
            DebugConsole.Push("COR", "ゲーム情報を読み込みます。");
            GameCollection games = _getTargetGames(settings);

            // テーマのロード
            DebugConsole.Push("COR", "テーマ情報を読み込みます。");
            LauncherTheme theme = _getLauncherTheme(settings);

            // メインウィンドウの表示
            DebugConsole.Push("COR", "メインウィンドウを初期化します。");
            MainForm mainForm = new MainForm();
            _formInitialize(mainForm, games);

            mainForm.LauncherTheme = theme;
            mainForm.Show();
            while (mainForm.Created)
            {
                //mainForm.GetLauncherPanel().Focus();
                Application.DoEvents();
                System.Threading.Thread.Sleep(1);
            }

            mainForm.Dispose();
            GC.Collect();
        }
    }
}
