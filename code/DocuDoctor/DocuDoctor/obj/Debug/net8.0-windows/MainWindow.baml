   M S B A M L   `   `   `  ����E  ADocuDoctor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:DocuDoctor
DocuDoctor  5   	       DocuDoctor.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:DocuDoctor     5   	   -   5          � Title$  
DocuDoctor��5   	   $	��450��5	   	   $	��800��6   ���#313638 � Icon$$ assets/DocumentationDoctor.ico��5   	   '  !System.Windows.Shell.WindowChrome   WindowChrome 5        5   
      GlassFrameThickness$ 0q�6      CornerRadius$ 15��6/      CaptionHeight$ 0Q�6A    � 	Resources 5         TitleBarButtonStyle �� 
TargetType �� Property	 �� Value
 �� 
TargetType ��Name �� Triggers P� Property P� Value �� 
TargetName��%  &	        �� 5   
   " ��6,   .�5      ��  � ��	 5      �� 5      "
 ��6&   .R�5      �� $ bdr_main��6!   $��1q�63   
������ 
������ ._�5      �� $��Center=�6/   $��Center=�6J   $��Content��6g   5       5      P� 5       �@J�IsMouseOver$
 True��6=   . �5   "   �� $ bdr_main��6)    � ��	 �#6c7a896U   5      P� 5       � ��$
 True��6;   . �5   "   �� $ bdr_main��6)    � ��	 �#6c7a896U   5      5       5"      5#      5$      .��5%      �  � RowDefinitions 5&   
   �� 5'      $��40 �6   �� 5(      $��* �6   .+�5*   
   � .+�5+      � $
��Left=�6   $��Center=�63   .+�5,      �� $%��/assets/DocumentationDoctor.png��6   $��20,0,0,0q�6A   $��Center=�6S   $��20��6n   $��20��6y   �� 5-      ���#f8f8ff$��18(�61   $��20,0,0,0q�6?   $��Center=�6Q   
DocuDoctor6x   5.      �� 5/       �� Orientation$ 
Horizontal=�6   $��Right=�62   .+�50      �� -   5         ��Name$ Minimize��50   F   #��[  6   $��50��6X   
������ 
������ .��51      �� $��/assets/minimize.png��6   $��20��6:   $��20��6E   52      �� 53      -   5        $ Maximize��53   F   #��[  6   $��50��6X   
������ 
������ .��54      �� $��/assets/maximize.png��6   $��20��6:   $��20��6E   55      �� 56      -   5        $ Close��56   F   #��[  6   $��50��6U   
������ 
������ .��57      �� $��/assets/close.png��6   $��20��67   $��20��6B   58      59      5:      5;      5<      