# tests/support/Golden.gd
class_name Golden
static func write(name:String, data:Variant) -> void:
  DirAccess.make_dir_recursive_absolute("res://tests/.golden")
  var f := FileAccess.open("res://tests/.golden/%s.json" % name, FileAccess.WRITE)
  f.store_string(JSON.stringify(data, "\t"))
  f.close()