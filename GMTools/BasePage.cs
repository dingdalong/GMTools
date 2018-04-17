using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GMTools
{
    public class BasePage : Page
    {
        public BasePage()
        {

        }

        /// <summary>
        /// 父节点
        /// </summary>
        private MainWindow parentWin;
        public MainWindow ParentWindow
        {
            get { return parentWin; }
            set { parentWin = value; }
        }

        /// <summary>
        /// 收到server的消息
        /// </summary>
        public virtual void GetMsg(string str)
        {

        }

        /// <summary>
        /// 当页签打开的时候
        /// </summary>
        public virtual void Open()
        {

        }

        /// <summary>
        /// 当页签关闭的时候
        /// </summary>
        public virtual void Close()
        {

        }
    }
}
