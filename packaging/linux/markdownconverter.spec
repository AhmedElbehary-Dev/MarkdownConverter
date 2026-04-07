Name:           markdownconverter
Version:        %{_version}
Release:        1%{?dist}
Summary:        Convert Markdown to PDF, Word, and Excel
License:        MIT
URL:            https://github.com/AhmedElbehary-Dev/MarkdownConverter
BuildArch:      x86_64
Requires:       libc.so.6()(64bit), libstdc++.so.6()(64bit)

%description
Markdown Converter Pro is a cross-platform desktop application that converts
Markdown files into PDF, Word (DOCX), and Excel (XLSX) formats. Features
include drag-and-drop support, quick paste functionality, and offline operation.

%install
mkdir -p %{buildroot}/usr/lib/markdownconverter
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/pixmaps
mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps

# Copy published files
cp -r %{_sourcedir}/publish/* %{buildroot}/usr/lib/markdownconverter/
chmod +x %{buildroot}/usr/lib/markdownconverter/MarkdownConverter

# Create symlink
ln -sf /usr/lib/markdownconverter/MarkdownConverter %{buildroot}/usr/bin/markdownconverter

# Desktop file
cat <<EOF > %{buildroot}/usr/share/applications/markdownconverter.desktop
[Desktop Entry]
Version=1.1
Type=Application
Name=Markdown Converter
GenericName=Markdown Converter
Comment=Convert Markdown to PDF, Word, and Excel
Exec=/usr/bin/markdownconverter %F
Icon=markdownconverter
Terminal=false
Categories=Utility;Office;TextEditor;Development;
MimeType=text/markdown;text/x-markdown;
Keywords=markdown;pdf;word;excel;converter;
StartupWMClass=MarkdownConverter
StartupNotify=true
EOF

# Icon - prefer PNG over ICO
if [ -f %{_sourcedir}/md_converter.png ]; then
    cp %{_sourcedir}/md_converter.png %{buildroot}/usr/share/pixmaps/markdownconverter.png
    cp %{_sourcedir}/md_converter.png %{buildroot}/usr/share/icons/hicolor/256x256/apps/markdownconverter.png
elif [ -f %{_sourcedir}/md_converter.ico ]; then
    cp %{_sourcedir}/md_converter.ico %{buildroot}/usr/share/pixmaps/markdownconverter.ico
fi

%post
# Update desktop database
if [ -x /usr/bin/update-desktop-database ]; then
    /usr/bin/update-desktop-database -q /usr/share/applications >/dev/null 2>&1 || :
fi

# Update icon cache
if [ -x /usr/bin/gtk-update-icon-cache ]; then
    /usr/bin/gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor >/dev/null 2>&1 || :
fi

# Ensure executable permissions
chmod +x /usr/lib/markdownconverter/MarkdownConverter >/dev/null 2>&1 || :

%preun
if [ "$1" -ge 1 ]; then
    # Upgrading - check if app is running
    if pgrep -f "MarkdownConverter" > /dev/null 2>&1; then
        echo "Warning: MarkdownConverter appears to be running."
        echo "It is recommended to close the application before upgrading."
    fi
fi

%postun
if [ "$1" -ge 1 ]; then
    # Upgrade - refresh desktop database and icon cache
    if [ -x /usr/bin/update-desktop-database ]; then
        /usr/bin/update-desktop-database -q /usr/share/applications >/dev/null 2>&1 || :
    fi
    if [ -x /usr/bin/gtk-update-icon-cache ]; then
        /usr/bin/gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor >/dev/null 2>&1 || :
    fi
elif [ "$1" -eq 0 ]; then
    # Uninstall - clean up everything
    rm -f /usr/share/applications/markdownconverter.desktop >/dev/null 2>&1 || :
    rm -f /usr/share/pixmaps/markdownconverter.* >/dev/null 2>&1 || :
    rm -f /usr/share/icons/hicolor/256x256/apps/markdownconverter.* >/dev/null 2>&1 || :
    rm -f /usr/bin/markdownconverter >/dev/null 2>&1 || :
    rm -rf /usr/lib/markdownconverter >/dev/null 2>&1 || :
    
    # Refresh desktop database and icon cache
    if [ -x /usr/bin/update-desktop-database ]; then
        /usr/bin/update-desktop-database -q /usr/share/applications >/dev/null 2>&1 || :
    fi
    if [ -x /usr/bin/gtk-update-icon-cache ]; then
        /usr/bin/gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor >/dev/null 2>&1 || :
    fi
fi

%files
/usr/lib/markdownconverter/
/usr/bin/markdownconverter
/usr/share/applications/markdownconverter.desktop
/usr/share/pixmaps/markdownconverter.*
/usr/share/icons/hicolor/256x256/apps/markdownconverter.*

%changelog
* %{date} MarkdownConverter Team
- Improved uninstallation cleanup with postun scripts
- Added desktop database and icon cache updates
- Enhanced desktop entry with MIME types and keywords
