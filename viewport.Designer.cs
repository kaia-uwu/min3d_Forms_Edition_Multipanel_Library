namespace min3d_Forms_Edition_Multipanel_Library
{
    partial class viewport
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
#if DEBUG
            debug_label = new System.Windows.Forms.Label();
#endif
            invalidation_timer = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // debug_label
            // 
#if DEBUG
            debug_label.AutoSize = true;
            debug_label.BackColor = System.Drawing.Color.Transparent;
            debug_label.Name = "debug_label";
            debug_label.Size = new System.Drawing.Size(0, 15);
            debug_label.TabIndex = 0;
#endif
            // 
            // invalidation_timer
            // 
            invalidation_timer.Interval = 16;
            invalidation_timer.Tick += tick_handler;
            // 
            // viewport
            // 
            DoubleBuffered = true;
            BackColor = System.Drawing.SystemColors.ControlLight;
#if DEBUG
            Controls.Add(debug_label);
#endif
            ResumeLayout(false);
            PerformLayout();
        }
#endregion

#if DEBUG
        private System.Windows.Forms.Label debug_label;
#endif
        private System.Windows.Forms.Timer invalidation_timer;
    }
}
